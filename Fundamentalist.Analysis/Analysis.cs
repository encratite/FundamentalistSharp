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

		private IMongoCollection<SecSubmission> _submissions;
		private IMongoCollection<TickerData> _tickers;
		private IMongoCollection<Price> _prices;

		private FilterDefinitionBuilder<SecSubmission> _submissionFilter = Builders<SecSubmission>.Filter;
		private FilterDefinitionBuilder<TickerData> _tickerFilter = Builders<TickerData>.Filter;
		private FilterDefinitionBuilder<Price> _priceFilter = Builders<Price>.Filter;

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
			_submissions = database.GetCollection<SecSubmission>("submissions");
			_tickers = database.GetCollection<TickerData>("tickers");
			_prices = database.GetCollection<Price>("prices");

			var singleTagStats = GetTagStats(null, database);
			var assetStats = GetTagStats("Assets", database);
			var revenueStats = GetTagStats("Revenues", database);
			using (var writer = new StreamWriter(_configuration.Output))
			{
				string json = JsonSerializer.Serialize(_configuration);
				writer.WriteLine("Configuration used:");
				writer.WriteLine(json);
				WriteCoefficients("Single tags", singleTagStats, writer);
				WriteCoefficients("Revenue quotients", revenueStats, writer);
				WriteCoefficients("Asset quotients", assetStats, writer);
			}
		}

		private List<TagStats> GetTagStats(string divisor, IMongoDatabase database)
		{
			string startMessage;
			if (divisor == null)
				startMessage = "Calculating single tag stats";
			else
				startMessage = $"Calculating tag quotient stats for divisor \"{divisor}\"";
			string endMessage = "Done calculating tag stats";
			using var timer = new PerformanceTimer(startMessage, endMessage);
			var formFiledFilter =
				_submissionFilter.Eq(x => x.Form, _configuration.Form) &
				_submissionFilter.Gte(x => x.Filed, _configuration.From.Value) &
				_submissionFilter.Lt(x => x.Filed, _configuration.To.Value);
			var filteredSubmission = _submissions.Find(formFiledFilter).ToList();
			var stats = new Dictionary<string, TagStats>();
			foreach (var submission in filteredSubmission)
			{
				var cikFilter = _tickerFilter.Eq(x => x.Cik, submission.Cik);
				foreach (var ticker in _tickers.Find(cikFilter).ToList())
					GetTickerStats(divisor, submission, ticker, stats);
			}
			foreach (var tagStats in stats.Values)
				tagStats.Frequency = (decimal)tagStats.Observations.Count / filteredSubmission.Count;
			var filteredTags = stats.Values.Where(x => x.Frequency >= _configuration.MinimumFrequency).ToList();
			Parallel.ForEach(filteredTags, stats =>
			{
				stats.SpearmanCoefficient = GetSpearmanCoefficient(stats.Observations);
				stats.PearsonCoefficient = GetPearsonCoefficient(stats.Observations);
			});
			return filteredTags;
		}

		private void GetTickerStats(string divisor, SecSubmission submission, TickerData ticker, Dictionary<string, TagStats> stats)
		{
			if (
				ticker == null ||
				ticker.MarketCap < _configuration.MinimumMarketCap ||
				ticker.MarketCap > _configuration.MaximumMarketCap
			)
				return;
			decimal? performance = GetPerformance(submission, ticker);
			if (!performance.HasValue)
				return;
			var filteredNumbers = submission.Numbers
				.Where(x => IsValidNumber(x, divisor))
				.OrderByDescending(x => x.EndDate);
			var usedTags = new HashSet<string>();
			var dateFilteredNumbers = new List<SecNumber>();
			foreach (var number in filteredNumbers)
			{
				string tag = number.Tag;
				if (usedTags.Contains(tag))
					continue;
				dateFilteredNumbers.Add(number);
				usedTags.Add(tag);
			}
			if (divisor == null)
				AddSingleTagStats(performance, dateFilteredNumbers, stats);
			else
				AddQuotientTagStats(divisor, performance, dateFilteredNumbers, stats);
		}

		private void AddSingleTagStats(decimal? performance, List<SecNumber> dateFilteredNumbers, Dictionary<string, TagStats> stats)
		{
			foreach (var number in dateFilteredNumbers)
			{
				var tagStats = GetTagStats(number.Tag, stats);
				var observation = new Observation(number.Value, performance.Value);
				tagStats.Observations.Add(observation);
			}
		}

		private void AddQuotientTagStats(string divisor, decimal? performance, List<SecNumber> dateFilteredNumbers, Dictionary<string, TagStats> stats)
		{
			var quotientNumber = dateFilteredNumbers.FirstOrDefault(x => x.Tag == divisor);
			if (quotientNumber == null || quotientNumber.Value == 0m)
				return;
			foreach (var number in dateFilteredNumbers)
			{
				string tag = number.Tag;
				if (tag == divisor)
					continue;
				var tagStats = GetTagStats(tag, stats);
				decimal quotient = number.Value / quotientNumber.Value;
				var observation = new Observation(quotient, performance.Value);
				tagStats.Observations.Add(observation);
			}
		}

		private TagStats GetTagStats(string tag, Dictionary<string, TagStats> stats)
		{
			TagStats tagStats;
			if (!stats.TryGetValue(tag, out tagStats))
			{
				tagStats = new TagStats(tag);
				stats[tag] = tagStats;
			}
			return tagStats;
		}

		private bool IsValidNumber(SecNumber number, string divisor)
		{
			bool isValidQuarter = number.Quarters == 0 ||
			(
				(_configuration.Form != "10-K" || number.Quarters == 4) &&
				(_configuration.Form != "10-Q" || number.Quarters == 1)
			);
			if (!isValidQuarter)
				return false;
			string unit = number.Unit;
			if (unit == "USD")
				return true;
			return
				divisor == null &&
				(
					unit == "shares" ||
					unit == "pure"
				);
		}

		private decimal? GetPerformance(SecSubmission submission, TickerData ticker)
		{
			var now = submission.Filed;
			var future = submission.Filed.AddMonths(_configuration.Horizon.Value);
			var price1 = GetClosePrice(ticker.Ticker, now);
			var price2 = GetClosePrice(ticker.Ticker, future);
			if (!price1.HasValue)
				return null;
			decimal priceQuotient = price2.HasValue ? price2.Value / price1.Value : 0m;
			var indexPrice1 = GetClosePrice(null, now);
			var indexPrice2 = GetClosePrice(null, future);
			if (!indexPrice1.HasValue || !indexPrice2.HasValue)
				return null;
			decimal indexQuotient = indexPrice2.Value / indexPrice1.Value;
			decimal performance = priceQuotient - indexQuotient;
			return performance;
		}

		private decimal? GetClosePrice(string ticker, DateTime date)
		{
			for (int i = 0; i <= 7; i++)
			{
				var filter =
					_priceFilter.Eq(x => x.Ticker, ticker) &
					_priceFilter.Eq(x => x.Date, date.AddDays(i));
				var price = _prices.Find(filter).FirstOrDefault();
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

		private double GetPearsonCoefficient(List<Observation> observations)
		{
			var xDeviations = GetDeviations(observations.Select(o => o.X));
			var yDeviations = GetDeviations(observations.Select(o => o.Y));
			double numerator = 0;
			for (int i = 0; i < xDeviations.Length; i++)
				numerator += (double)xDeviations[i] * (double)yDeviations[i];
			double xRootSquareSum = GetRootSquareSum(xDeviations);
			double yRootSquareSum = GetRootSquareSum(yDeviations);
			double output = numerator / (xRootSquareSum * yRootSquareSum);
			return output;
		}

		private decimal[] GetDeviations(IEnumerable<decimal> input)
		{
			decimal mean = GetMean(input);
			var output = input.Select(x => x - mean).ToArray();
			return output;
		}

		private decimal GetMean(IEnumerable<decimal> input)
		{
			int n = 0;
			decimal sum = 0m;
			foreach (decimal x in input)
			{
				sum += x;
				n++;
			}
			decimal mean = sum / n;
			return mean;
		}

		private double GetRootSquareSum(IEnumerable<decimal> input)
		{
			double sum = 0;
			foreach (var x in input)
			{
				double doubleX = (double)x;
				sum += doubleX * doubleX;
			}
			double output = Math.Sqrt((double)sum);
			return output;
		}

		private int[] GetRanks(List<Observation> observations, bool selectX)
		{
			int i = 1;
			var indexFloats = observations.Select(o => new IndexValue(selectX ? o.X : o.Y, i++)).OrderBy(x => x.Value);
			int[] output = indexFloats.Select(x => x.Index).ToArray();
			return output;
		}

		private void WriteCoefficients(string title, List<TagStats> tags, StreamWriter writer)
		{
			writer.WriteLine(string.Empty);
			writer.WriteLine($"{title}:");
			int i = 1;
			foreach (var stats in tags.OrderByDescending(x => x.PearsonCoefficient.Value))
			{
				var statsStrings = new List<string>();
				statsStrings.Add($"Pearson {stats.PearsonCoefficient:F3}");
				statsStrings.Add($"Spearman {stats.SpearmanCoefficient.Value:F3}");
				statsStrings.Add($"frequency {stats.Frequency:P2}");
				statsStrings.Add($"{stats.Observations.Count} samples");
				writer.WriteLine($"{i}. {stats.Name} ({string.Join(", ", statsStrings)})");
				i++;
			}
		}
	}
}
