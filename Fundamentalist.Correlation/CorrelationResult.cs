namespace Fundamentalist.Correlation
{
	internal class CorrelationResult
	{
		public string Feature { get; private set; }
		public decimal Coefficient { get; private set; }

		public CorrelationResult(string feature, decimal coefficient)
		{
			Feature = feature;
			Coefficient = coefficient;
		}
	}
}
