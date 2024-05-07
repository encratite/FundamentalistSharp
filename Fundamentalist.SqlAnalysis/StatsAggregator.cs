using System.Collections.Concurrent;

namespace Fundamentalist.SqlAnalysis
{
	internal class StatsAggregator
	{
		private ConcurrentBag<decimal> _values = new ConcurrentBag<decimal>();
		private decimal? _mean = null;
		private double? _standardDeviation = null;

		public int Count => _values.Count;

		public decimal Mean
		{
			get
			{
				UpdateStats();
				return _mean.Value;
			}
		}

		public double StandardDeviation
		{
			get
			{
				UpdateStats();
				return _standardDeviation.Value;
			}
		}

		public StatsAggregator(decimal value)
		{
			_values.Add(value);
		}

		public void Add(decimal value)
		{
			_values.Add(value);
			_mean = null;
			_standardDeviation = null;
		}

		public override string ToString()
		{
			return $"{Mean:F2}";
		}

		public void UpdateStats()
		{
			if (!_values.Any() || _mean.HasValue)
				return;
			_mean = _values.Sum() / _values.Count;
			decimal sum = 0m;
			foreach (decimal value in _values)
			{
				decimal delta = value - _mean.Value;
				sum += delta * delta;
			}
			_standardDeviation = Math.Sqrt((double)(sum / _values.Count));
		}
	}
}
