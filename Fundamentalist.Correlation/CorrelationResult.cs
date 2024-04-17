namespace Fundamentalist.Correlation
{
	internal class CorrelationResult
	{
		public string Feature { get; private set; }
		public decimal Coefficient { get; private set; }
		public int Observations { get; private set; }

		public CorrelationResult(string feature, decimal coefficient, int observations)
		{
			Feature = feature;
			Coefficient = coefficient;
			Observations = observations;
		}
	}
}
