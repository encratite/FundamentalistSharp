using Fundamentalist.Common.Document;

namespace Fundamentalist.Backtest.Strategies
{
	internal class TickerPerformance
	{
		public string Ticker { get; set; }
		public List<Price> Prices { get; set; }
		public double? AdjustedSlope { get; set; }
		public double? MovingAverage { get; set; }
		public decimal LargestGap { get; set; }
		public decimal AverageTrueRange { get; set; }

		public TickerPerformance(string ticker, List<Price> prices, ClenowMomentumConfiguration configuration)
		{
			Ticker = ticker;
			Prices = prices;
			AdjustedSlope = GetAdjustedSlope(configuration.RegressionSlopeDays);
			MovingAverage = GetMovingAverage(configuration.StockMovingAverageDays);
			LargestGap = GetLargestGap(configuration.RegressionSlopeDays);
			AverageTrueRange = GetAverageTrueRange(configuration.AverageTrueRangeDays);
		}

		public decimal GetLastClose()
		{
			return Prices.Last().Close;
		}

		public override string ToString()
		{
			return $"{Ticker} ({AdjustedSlope}, {MovingAverage}, {LargestGap}, {AverageTrueRange})";
		}

		private double? GetAdjustedSlope(int slopeDays)
		{
			if (Prices.Count < slopeDays)
				return null;
			var prices = GetPrices(slopeDays);
			var xValues = new List<double>();
			for (int i = 1; i <= slopeDays; i++)
				xValues.Add(i);
			var xDeltas = GetDeltas(xValues);
			var yValues = prices.Select(x => Math.Log((double)x));
			var yDeltas = GetDeltas(yValues);
			double slope = GetSlope(xDeltas, yDeltas);
			double annualizedSlope = Math.Pow(Math.Exp(slope), 250) - 1;
			double r2 = GetR2(xDeltas, yDeltas);
			double adjustedSlope = annualizedSlope * r2;
			return adjustedSlope;
		}

		private double? GetMovingAverage(int movingAverageDays)
		{
			if (Prices.Count < movingAverageDays)
				return null;
			var prices = GetPrices(movingAverageDays);
			double movingAverage = GetMean(prices.Select(x => (double)x));
			return movingAverage;
		}

		private List<decimal> GetPrices(int count)
		{
			return Prices
				.AsEnumerable()
				.TakeLast(count)
				.Select(x => x.Close)
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

		private double GetSlope(List<double> xDeltas, List<double> yDeltas)
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

		private decimal GetLargestGap(int slopeDays)
		{
			decimal reference = GetLastClose();
			decimal largestGap = 0;
			var prices = Prices.TakeLast(slopeDays);
			foreach (var price in prices)
			{
				var values = new decimal[]
				{
					price.Open,
					price.High,
					price.Low,
					price.Close
				};
				foreach (decimal value in values)
				{
					decimal gap = Math.Abs(value / reference - 1);
					largestGap = Math.Max(gap, largestGap);
				}
			}
			return largestGap;
		}

		private decimal GetAverageTrueRange(int averageTrueRangeDays)
		{
			var prices = Prices.TakeLast(averageTrueRangeDays + 1).ToArray();
			decimal sum = 0;
			for (int i = 1; i < prices.Length; i++)
			{
				var current = prices[i];
				var previous = prices[i - 1];
				decimal trueRange = current.High - current.Low;
				trueRange = Math.Max(Math.Abs(current.High - previous.Close), trueRange);
				trueRange = Math.Max(Math.Abs(current.Low - previous.Close), trueRange);
				sum += trueRange;
			}
			decimal averageTrueRange = sum / averageTrueRangeDays;
			return averageTrueRange;
		}
	}
}
