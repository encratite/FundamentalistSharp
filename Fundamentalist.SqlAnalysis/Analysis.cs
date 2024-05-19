using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using System.Text.Json;

namespace Fundamentalist.Analysis
{
	internal class Analysis
	{
		private Configuration _configuration;

		public void Run(Configuration configuration)
		{
			_configuration = configuration;
			var pack = new ConventionPack
			{
				new CamelCaseElementNameConvention()
			};
			ConventionRegistry.Register(nameof(CamelCaseElementNameConvention), pack, _ => true);
			var client = new MongoClient(_configuration.ConnectionString);
			var database = client.GetDatabase("fundamentalist");

			using (var writer = new StreamWriter(_configuration.Output))
			{
				string json = JsonSerializer.Serialize(_configuration);
				writer.WriteLine("Configuration used:");
				writer.WriteLine(json);
				/*
				WriteCoefficients("Single tags", singleTagStats, false, writer);
				WriteCoefficients("Revenue quotients", revenueStats, true, writer);
				WriteCoefficients("Asset quotients", assetStats, true, writer);
				*/
			}
		}

		private List<TagStats> GetSingleTagStats(IMongoDatabase database)
		{
			throw new NotImplementedException();
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
