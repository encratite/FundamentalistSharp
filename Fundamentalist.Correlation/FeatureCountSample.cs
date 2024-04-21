namespace Fundamentalist.Correlation
{
	internal class FeatureCountSample
	{
		public int Count { get; set; }
		public float Performance { get; set; }

		public FeatureCountSample(int count, float performance)
		{
			Count = count;
			Performance = performance;
		}
	}
}
