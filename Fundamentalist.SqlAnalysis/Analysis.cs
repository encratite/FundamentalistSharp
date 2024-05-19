using Amazon.SecurityToken.Model;
using Fundamentalist.Common;
using Fundamentalist.Common.Document;
using Fundamentalist.CsvImport.Document;
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
			var singleTagStats = GetSingleTagStats(database);
			using (var writer = new StreamWriter(_configuration.Output))
			{
				string json = JsonSerializer.Serialize(_configuration);
				writer.WriteLine("Configuration used:");
				writer.WriteLine(json);
				WriteCoefficients("Single tags", singleTagStats, false, writer);
				/*
				WriteCoefficients("Revenue quotients", revenueStats, true, writer);
				WriteCoefficients("Asset quotients", assetStats, true, writer);
				*/
			}
		}

		private List<TagStats> GetSingleTagStats(IMongoDatabase database)
		{
			using var timer = new PerformanceTimer("Calculating single tag stats", "Done calculating single tag stats");
			var submissions = database.GetCollection<SecSubmission>("submissions");
			var submissionFilter = Builders<SecSubmission>.Filter;
			var tickers = database.GetCollection<TickerData>("tickers");
			var tickerFilter = Builders<TickerData>.Filter;
			var prices = database.GetCollection<Price>("prices");
			var formFiledFilter =
				submissionFilter.Eq(x => x.Form, _configuration.Form) &
				submissionFilter.Gte(x => x.Filed, _configuration.From.Value) &
				submissionFilter.Lt(x => x.Filed, _configuration.To.Value);
			var filteredSubmission = submissions.Find(formFiledFilter).ToList();
			var stats = new Dictionary<string, TagStats>();
			foreach (var submission in filteredSubmission)
			{
				var cikFilter = tickerFilter.Eq(x => x.Cik, submission.Cik);
				foreach (var ticker in tickers.Find(cikFilter).ToList())
				{
					if (ticker == null || ticker.MarketCap < 3)
						continue;
					var now = submission.Filed;
					var future = submission.Filed.AddMonths(_configuration.Horizon.Value);
					var price1 = GetClosePrice(ticker.Ticker, now, prices);
					var price2 = GetClosePrice(ticker.Ticker, future, prices);
					decimal priceQuotient;
					if (price1.HasValue && price2.HasValue)
						priceQuotient = price2.Value / price1.Value;
					else
						priceQuotient = 0m;
					var indexPrice1 = GetClosePrice(null, now, prices);
					var indexPrice2 = GetClosePrice(null, future, prices);
					if (!indexPrice1.HasValue || !indexPrice2.HasValue)
						continue;
					decimal indexQuotient = indexPrice2.Value / indexPrice1.Value;
					decimal performance = priceQuotient - indexQuotient;
					var filteredNumbers = submission.Numbers.Where(x =>
						x.Quarters == 0 ||
						(
							(_configuration.Form != "10-K" || x.Quarters == 4) &&
							(_configuration.Form != "10-Q" || x.Quarters == 1)
						)
					).OrderByDescending(x => x.EndDate);
					var usedTags = new HashSet<string>();
					foreach (var number in filteredNumbers)
					{
						string unit = number.Unit;
						if (
							unit != "USD" &&
							unit != "shares" &&
							unit != "pure"
						)
							continue;
						string tag = number.Tag;
						if (usedTags.Contains(tag))
							continue;
						TagStats tagStats;
						if (!stats.TryGetValue(tag, out tagStats))
						{
							tagStats = new TagStats(tag);
							stats[tag] = tagStats;
						}
						var observation = new Observation(number.Value, performance);
						tagStats.Observations.Add(observation);
						usedTags.Add(tag);
					}
				}
			}
			var filteredTags = stats.Values.Where(x => x.Observations.Count >= _configuration.MinimumFrequency).ToList();
			Parallel.ForEach(filteredTags, stats =>
			{
				stats.SpearmanCoefficient = GetSpearmanCoefficient(stats.Observations);
				stats.Covariance = GetCovariance(stats.Observations);
			});
			return filteredTags;
		}

		private decimal? GetClosePrice(string ticker, DateTime date, IMongoCollection<Price> prices)
		{
			for (int i = 0; i <= 7; i++)
			{
				var filter = Builders<Price>.Filter.Eq(x => x.Ticker, ticker) & Builders<Price>.Filter.Eq(x => x.Date, date.AddDays(i));
				var price = prices.Find(filter).FirstOrDefault();
				if (price != null)
					return price.Close;
			}
			return null;
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
				writer.WriteLine($"{i}. {stats.Name} ({string.Join(", ", statsStrings)})");
				i++;
			}
		}
	}
}
