using Fundamentalist.Common;
using MySql.Data.MySqlClient;
using System.Data.SqlTypes;
using System.Text.Json;

namespace Fundamentalist.SqlAnalysis
{
	internal class SqlAnalysis
	{
		private Configuration _configuration;

		public void Run(Configuration configuration)
		{
			_configuration = configuration;
			List<TagStats> singleTagStats, revenueStats, assetStats;
			using (var connection = new MySqlConnection(_configuration.ConnectionString))
			{
				connection.Open();
				using (new PerformanceTimer("Loading single stats", "Done loading single stats"))
					singleTagStats = GetSingleTagStats(connection);
				using (new PerformanceTimer("Loading revenue stats", "Done loading revenue stats"))
					revenueStats = GetTagRatioStats("Revenues", connection);
				using (new PerformanceTimer("Loading asset stats", "Done loading asset stats"))
					assetStats = GetTagRatioStats("Assets", connection);
			}
			
			using (var writer = new StreamWriter(_configuration.Output))
			{
				string json = JsonSerializer.Serialize(_configuration);
				writer.WriteLine("Configuration used:");
				writer.WriteLine(json);
				WriteCoefficients("Single tags", singleTagStats, false, writer);
				WriteCoefficients("Revenue quotients", revenueStats, true, writer);
				WriteCoefficients("Asset quotients", assetStats, true, writer);
			}
		}

		private List<TagStats> GetSingleTagStats(MySqlConnection connection)
		{
			string query = "call get_tag_performance(@from, @to, @form, @horizon);";
			var command = new MySqlCommand(query, connection);
			SetCommonParameters(command);
			var stats = GetStats(command);
			return stats;
		}

		private List<TagStats> GetTagRatioStats(string divisor, MySqlConnection connection)
		{
			var tags = new Dictionary<string, TagStats>();
			string query = "call get_tag_ratio_performance(@divisor, @from, @to, @form, @horizon);";
			var command = new MySqlCommand(query, connection);
			SetCommonParameters(command);
			command.Parameters.AddWithValue("@divisor", divisor);
			var stats = GetStats(command);
			return stats;
		}

		private List<TagStats> GetStats(MySqlCommand command)
		{
			using var reader = command.ExecuteReader();
			var tags = new Dictionary<string, TagStats>();
			while (reader.Read())
			{
				try
				{
					string name = reader.GetString("tag");
					decimal value = reader.GetDecimal("value");
					decimal performance = reader.GetDecimal("performance");
					TagStats stats;
					if (!tags.TryGetValue(name, out stats))
					{
						stats = new TagStats(name);
						tags[name] = stats;
					}
					var observation = new Observation(value, performance);
					stats.Observations.Add(observation);
				}
				catch (SqlNullValueException)
				{
				}
			}
			var filteredTags = tags.Values.Where(x => x.Observations.Count >= _configuration.MinimumFrequency).ToList();
			Parallel.ForEach(filteredTags, stats =>
			{
				stats.SpearmanCoefficient = GetSpearmanCoefficient(stats.Observations);
				stats.Covariance = GetCovariance(stats.Observations);
			});
			return filteredTags;
		}

		private void SetCommonParameters(MySqlCommand command)
		{
			command.CommandTimeout = 3600;
			var parameters = command.Parameters;
			parameters.AddWithValue("@from", _configuration.From);
			parameters.AddWithValue("@to", _configuration.To);
			parameters.AddWithValue("@form", _configuration.Form);
			parameters.AddWithValue("@horizon", _configuration.Horizon);
		}

		private decimal GetSpearmanCoefficient(List<Observation> observations)
		{
			decimal n = observations.Count;
			var xRanks = GetRanks(observations, true);
			var yRanks = GetRanks(observations, false);
			decimal sum = 0;
			for (int i = 0; i < xRanks.Length; i++)
			{
				decimal difference = xRanks[i] - yRanks[i];
				sum += difference * difference;
			}
			decimal coefficient = 1m - 6m * sum / n / (n * n - 1m);
			return coefficient;
		}

		private decimal GetCovariance(List<Observation> observations)
		{
			decimal sum = 0;
			var array = observations.ToArray();
			for (int i = 0; i < array.Length; i++)
			{
				for (int j = i + 1; j < array.Length; j++)
				{
					var o1 = array[i];
					var o2 = array[j];
					decimal dx = o1.X - o2.X;
					decimal dy = o1.Y - o2.Y;
					sum += dx * dy;
				}
			}
			decimal covariance = sum / (array.Length * array.Length);
			return covariance;
		}

		private int[] GetRanks(List<Observation> observations, bool selectX)
		{
			int i = 1;
			var indexFloats = observations.Select(o => new IndexValue(selectX ? o.X : o.Y, i++)).OrderBy(x => x.Value);
			int[] output = indexFloats.Select(x => x.Index).ToArray();
			return output;
		}

		private void WriteCoefficients(string title, List<TagStats> tags, bool enableCovariance, StreamWriter writer)
		{
			writer.WriteLine(string.Empty);
			writer.WriteLine($"{title}:");
			int i = 1;
			foreach (var stats in tags.OrderByDescending(x => x.SpearmanCoefficient.Value))
			{
				var statsStrings = new List<string>();
				statsStrings.Add($"Spearman {stats.SpearmanCoefficient.Value:F3}");
				if (enableCovariance)
					statsStrings.Add($"covariance {stats.Covariance:F3}");
				statsStrings.Add($"{stats.Observations.Count} samples");
				writer.WriteLine($"{i}. {stats.Name} ({string.Join(" ", statsStrings)})");
				i++;
			}
		}
	}
}
