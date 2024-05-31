using Fundamentalist.Common.Document;

namespace Fundamentalist.Backtest.Strategies
{
	internal class TickerPerformance
	{
		public string Ticker { get; set; }
		public List<Price> Prices { get; set; }
		public double? AdjustedSlope { get; set; }
		public double? MovingAverage { get; set; }
		public decimal? AverageTrueRange { get; set; }

		public TickerPerformance(string ticker, List<Price> prices, int slopeDays, int movingAverageDays)
		{
			Ticker = ticker;
			Prices = prices;
			AdjustedSlope = GetAdjustedSlope(slopeDays);
			MovingAverage = GetMovingAverage(movingAverageDays);
		}

		public decimal GetLastClose()
		{
			return Prices.Last().UnadjustedClose.Value;
		}

		private double? GetAdjustedSlope(int slopeDays)
		{
			if (Prices.Count < slopeDays)
				return null;
			var prices = GetPrices(slopeDays);
			var xValues = prices.Select(x => Math.Log((double)x));
			var xDeltas = GetDeltas(xValues);
			var yValues = new List<double>();
			for (int i = 1; i <= slopeDays; i++)
				yValues.Add(i);
			var yDeltas = GetDeltas(yValues);
			double exponentialSlope = GetExponentialSlope(xDeltas, yDeltas);
			double annualizedSlope = Math.Pow(exponentialSlope, 250) - 1;
			double r2 = GetR2(xDeltas, yDeltas);
			double adjustedSlope = annualizedSlope * r2;
			return adjustedSlope;
		}

		private double? GetMovingAverage(int movingAverageDays)
		{
			if (Prices.Count < movingAverageDays)
				return null;
			var prices = GetPrices(movingAverageDays);
			double movingAverage = GetMean(prices.Cast<double>());
			return movingAverage;
		}

		private List<decimal> GetPrices(int count)
		{
			return Prices
				.AsEnumerable()
				.Reverse()
				.Take(count)
				.Select(x => x.UnadjustedClose.Value)
				.ToList();
		}

		private List<double> GetDeltas(IEnumerable<double> values)
		{
			double mean = GetMean(values);
			var deltas = values.Select(x => x - mean).ToList();
			return deltas;
		}

		private double GetMean(IEnumerable<double> values)
		{
			double sum = 0;
			foreach (double x in values)
				sum += (double)x;
			double mean = sum / values.Count();
			return mean;
		}

		private double GetExponentialSlope(List<double> xDeltas, List<double> yDeltas)
		{
			double numerator = 0;
			double denominator = 0;
			for (int i = 0; i < xDeltas.Count; i++)
			{
				double dx = xDeltas[i];
				double dy = yDeltas[i];
				numerator += dx * dy;
				denominator += dx * dx;
			}
			double exponentialSlope = numerator / denominator;
			return exponentialSlope;
		}

		private double GetR2(List<double> xDeltas, List<double> yDeltas)
		{
			double numerator = 0;
			double denominator = 0;
			for (int i = 0; i < xDeltas.Count; i++)
			{
				double dx = xDeltas[i];
				double dy = yDeltas[i];
				numerator += dx * dy;
				denominator += Math.Sqrt(dx * dx * dy * dy);
			}
			double r2 = numerator / denominator;
			return r2;
		}
	}
}
