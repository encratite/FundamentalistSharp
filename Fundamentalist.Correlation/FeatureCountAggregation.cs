namespace Fundamentalist.Correlation
{
	internal class FeatureCountAggregation
	{
		public List<float> Performance { get; set; } = new List<float>();
		public List<decimal> Prices { get; set; } = new List<decimal>();
		public List<decimal> Volumes { get; set; } = new List<decimal>();
	}
}
