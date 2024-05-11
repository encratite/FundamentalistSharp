using Fundamentalist.Common;
using System.Diagnostics;

namespace Fundamentalist.LeadLag
{
	internal class LeadLagDetector
	{
		private DateOnly _from;
		private DateOnly _to;
		private int _lag;
		private List<TickerData> _tickers = new List<TickerData>();

		public LeadLagDetector(DateOnly from, DateOnly to, int lag)
		{
			_from = from;
			_to = to;
			_lag = lag;
		}

		public void Run(string priceDataDirectory, string outputPath)
		{
			Console.WriteLine("Loading price data");
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			var files = Directory.GetFiles(priceDataDirectory, "*.csv");
			foreach (string file in files)
			{
				string ticker = Path.GetFileNameWithoutExtension(file);
				var priceData = DataReader.GetPriceData(ticker, priceDataDirectory, _from, null);
				if (priceData == null)
					continue;
				var tickerData = new TickerData(ticker, priceData);
				_tickers.Add(tickerData);
			}
			stopwatch.Stop();
			Console.WriteLine($"Loaded {_tickers.Count} tickers in {stopwatch.Elapsed.TotalSeconds:F1} s");
			stopwatch.Restart();
			Console.WriteLine("Calculating coefficients");
			var results = new List<CorrelationResult>();
			int processed = 0;
			Parallel.For(0, _tickers.Count, i =>
			{
				int currentProcessed = Interlocked.Increment(ref processed);
				if (currentProcessed % 100 == 0)
					Console.WriteLine($"Progress: {currentProcessed}/{_tickers.Count}");
				var ticker1 = _tickers[i];
				var priceData1 = ticker1.PriceData;
				for (int j = 0; j < _tickers.Count; j++)
				{
					if (i == j)
						continue;
					var ticker2 = _tickers[j];
					var priceData2 = ticker2.PriceData;
					if (Math.Abs(priceData1.Count - priceData2.Count) > 20)
						continue;
					if (!IsValidPriceData(priceData1) || !IsValidPriceData(priceData2))
						continue;
					var observations1 = GetObservations(priceData1, priceData2, null, _to);
					var observations2 = GetObservations(priceData1, priceData2, _to, null);
					const int MinimumObservations = 100;
					if (observations1.Length < MinimumObservations || observations2.Length < MinimumObservations)
						continue;
					decimal coefficient1 = GetSpearmanCoefficient(observations1);
					decimal coefficient2 = GetSpearmanCoefficient(observations2);
					var result = new CorrelationResult(ticker1.Name, ticker2.Name, coefficient1, coefficient2, observations1.Length);
					results.Add(result);
				}
			});
			stopwatch.Stop();
			Console.WriteLine($"Calculated coefficients in {stopwatch.Elapsed.TotalSeconds:F1} s");
			using (var writer = new StreamWriter(outputPath))
			{
				int limit = 100;
				var orderedResults = results.OrderByDescending(x => x.Coefficient1).Take(limit).Concat(results.OrderBy(x => x.Coefficient1).Take(limit).Reverse());
				foreach (var result in orderedResults)
					writer.WriteLine($"{result.Ticker1}, {result.Ticker2}: {result.Coefficient1:F3} -> {result.Coefficient2:F3} ({result.Observations} observations)");
			}
		}

		private bool IsValidPriceData(SortedList<DateOnly, PriceData> priceData)
		{
			const decimal Limit = 10.0m;
			const decimal MinimumVolume = 1e6m;
			int i = 0;
			foreach (var pair in priceData)
			{
				if (i >= 10)
					break;
				var price = pair.Value;
				if (price.Open < Limit || price.Close < Limit || price.Open * price.Volume < MinimumVolume)
					return false;
				i++;
			}
			return true;
		}

		private Observation[] GetObservations(SortedList<DateOnly, PriceData> priceData1, SortedList<DateOnly, PriceData> priceData2, DateOnly? from, DateOnly? to)
		{
			var observations = new List<Observation>();
			foreach (var pair in priceData1)
			{
				var date1 = pair.Key;
				var price1 = pair.Value;
				var date2 = date1.AddDays(_lag);
				if (from.HasValue && (date1 < from.Value || date2 < from.Value))
					continue;
				if (to.HasValue && (date1 >= to.Value || date2 >= to.Value))
					continue;
				PriceData price2;
				if (!priceData2.TryGetValue(date2, out price2))
					continue;
				decimal x = price1.Close / price1.Open;
				decimal y = price2.Close / price2.Open;
				var observation = new Observation(x, y);
				observations.Add(observation);
			}
			return observations.ToArray();
		}

		private decimal GetSpearmanCoefficient(Observation[] observations)
		{
			decimal n = observations.Length;
			var xRanks = GetRanks(observations, o => o.X);
			var yRanks = GetRanks(observations, o => o.Y);
			decimal squareSum = 0;
			for (int i = 0; i < xRanks.Length; i++)
			{
				decimal difference = xRanks[i] - yRanks[i];
				squareSum += difference * difference;
			}
			decimal coefficient = 1m - 6m * squareSum / n / (n * n - 1m);
			return coefficient;
		}

		private int[] GetRanks(Observation[] observations, Func<Observation, decimal> select)
		{
			int i = 1;
			var indexFloats = observations.Select(x => new Indexed(select(x), i++)).OrderBy(x => x.Value);
			int[] output = indexFloats.Select(x => x.Index).ToArray();
			return output;
		}
	}
}
