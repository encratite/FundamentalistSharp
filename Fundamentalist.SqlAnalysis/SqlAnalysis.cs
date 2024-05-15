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
			const string Revenues = "Revenues";
			const string Assets = "Assets";

			_configuration = configuration;
			List<FactStats> revenueStats, assetStats;
			using (var connection = new MySqlConnection(_configuration.ConnectionString))
			{
				connection.Open();
				revenueStats = GetFactStats(Revenues, connection);
				assetStats = GetFactStats(Assets, connection);
			}
			
			using (var writer = new StreamWriter(_configuration.Output))
			{
				string json = JsonSerializer.Serialize(_configuration);
				writer.WriteLine("Configuration used:");
				writer.WriteLine(json);
				WriteCoefficients(Revenues, revenueStats, writer);
				WriteCoefficients(Assets, assetStats, writer);
			}
		}

		private List<FactStats> GetFactStats(string divisor, MySqlConnection connection)
		{
			var facts = new Dictionary<string, FactStats>();
			string query = "call get_facts_by_ratio(@divisor, @from, @to, @form, @horizon);";
			var command = new MySqlCommand(query, connection);
			command.CommandTimeout = 3600;
			var parameters = command.Parameters;
			parameters.AddWithValue("@divisor", divisor);
			parameters.AddWithValue("@from", _configuration.From);
			parameters.AddWithValue("@to", _configuration.To);
			parameters.AddWithValue("@form", _configuration.Form);
			parameters.AddWithValue("@horizon", _configuration.Horizon);
			using var reader = command.ExecuteReader();
			while (reader.Read())
			{
				try
				{
					string name = reader.GetString("name");
					decimal ratio = reader.GetDecimal("ratio");
					decimal performance = reader.GetDecimal("performance");
					FactStats stats;
					if (!facts.TryGetValue(name, out stats))
					{
						stats = new FactStats(name);
						facts[name] = stats;
					}
					var ratioPerformance = new Observation(ratio, performance);
					stats.Observations.Add(ratioPerformance);
				}
				catch (SqlNullValueException)
				{
				}
			}
			var filteredFacts = facts.Values.Where(x => x.Observations.Count >= _configuration.MinimumFrequency).ToList();
			Parallel.ForEach(filteredFacts, stats =>
			{
				stats.SpearmanCoefficient = GetSpearmanCoefficient(stats.Observations);
			});
			return filteredFacts;
		}

		private decimal GetSpearmanCoefficient(List<Observation> observations)
		{
			decimal n = observations.Count;
			var xRanks = GetRanks(observations, true);
			var yRanks = GetRanks(observations, false);
			decimal squareSum = 0;
			for (int i = 0; i < xRanks.Length; i++)
			{
				decimal difference = xRanks[i] - yRanks[i];
				squareSum += difference * difference;
			}
			decimal coefficient = 1m - 6m * squareSum / n / (n * n - 1m);
			return coefficient;
		}

		private int[] GetRanks(List<Observation> observations, bool selectX)
		{
			int i = 1;
			var indexFloats = observations.Select(o => new IndexValue(selectX ? o.X : o.Y, i++)).OrderBy(x => x.Value);
			int[] output = indexFloats.Select(x => x.Index).ToArray();
			return output;
		}

		private void WriteCoefficients(string divisor, List<FactStats> facts, StreamWriter writer)
		{
			writer.WriteLine(string.Empty);
			writer.WriteLine($"Spearman correlation for \"{divisor}\":");
			int i = 1;
			foreach (var stats in facts.OrderByDescending(x => x.SpearmanCoefficient.Value))
			{
				writer.WriteLine($"{i}. {stats.Name} ({stats.SpearmanCoefficient.Value:F3}, {stats.Observations.Count} samples)");
				i++;
			}
		}
	}
}
