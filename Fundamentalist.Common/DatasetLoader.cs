﻿namespace Fundamentalist.Common
{
	public class DatasetLoader
	{
		public Dictionary<string, TickerCacheEntry> Cache { get; private set; }

		public int BadTickers
		{
			get => _badTickers;
		}

		public int GoodTickers
		{
			get => _goodTickers;
		}

		public int _badTickers;
		private int _goodTickers;

		public void Load(string earningsPath, string priceDataDirectory, int? features = 1000, int priceDataMinimum = 200, DateOnly? from = null, DateOnly? to = null, HashSet<int> featureSelection = null)
		{
			Cache = new Dictionary<string, TickerCacheEntry>();
			_badTickers = 0;
			_goodTickers = 0;
			LoadEarnings(earningsPath, features, from, to, featureSelection);
			DateOnly? priceFrom = from;
			if (priceFrom.HasValue)
			{
				// Enable common SMA/EMA calculations
				priceFrom = new DateOnly(priceFrom.Value.Year - 1, priceFrom.Value.Month, priceFrom.Value.Day);
			}
			LoadPriceData(priceDataDirectory, priceDataMinimum, priceFrom, to);
		}

		private void LoadEarnings(string earningsPath, int? features, DateOnly? from, DateOnly? to, HashSet<int> featureSelection)
		{
			var earningLines = DataReader.GetEarnings(earningsPath, features, from, to, featureSelection);
			foreach (var x in earningLines)
			{
				string ticker = x.Ticker;
				TickerCacheEntry cacheEntry;
				if (!Cache.TryGetValue(ticker, out cacheEntry))
				{
					cacheEntry = new TickerCacheEntry();
					Cache[ticker] = cacheEntry;
				}
				cacheEntry.Earnings.Add(x.Date, x.Features);
			}
		}

		private void LoadPriceData(string priceDataDirectory, int priceDataMinimum, DateOnly? from, DateOnly? to)
		{
			var options = new ParallelOptions
			{
				MaxDegreeOfParallelism = 4
			};
			Parallel.ForEach(Cache.ToList(), options, x =>
			{
				string ticker = x.Key;
				var cacheEntry = x.Value;
				var priceData = DataReader.GetPriceData(ticker, priceDataDirectory, from, to);
				if (priceData == null || priceData.Count < priceDataMinimum)
				{
					Cache.Remove(ticker);
					Interlocked.Increment(ref _badTickers);
					return;
				}
				cacheEntry.PriceData = priceData;
				Interlocked.Increment(ref _goodTickers);
			});
		}
	}
}
