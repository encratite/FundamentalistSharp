namespace Fundamentalist.SqlAnalysis
{
	internal class MeanAggregator
	{
		private decimal _sum = 0m;
		private int _count = 0;

		public decimal Mean => _sum / _count;

		public MeanAggregator(decimal value)
		{
			Add(value);
		}

		public void Add(decimal value)
		{
			_sum += value;
			_count++;
		}

		public override string ToString()
		{
			return $"{Mean:F2}";
		}
	}
}
