namespace Fundamentalist.Correlation
{
	internal class FeatureCountSample
	{
		public int Count { get; set; }
		public float Performance { get; set; }
		public decimal Price { get; set; }
		public long Volume { get; set; }

		public FeatureCountSample(int count, float performance, decimal price, long volume)
		{
			Count = count;
			Performance = performance;
			Price = price;
			Volume = volume;
		}
	}
}
