namespace Fundamentalist.LeadLag
{
	internal class CorrelationResult
	{
		public string Ticker1 { get; set; }
		public string Ticker2 { get; set; }
		public decimal Coefficient1 { get; private set; }
		public decimal Coefficient2 { get; private set; }
		public int Observations { get; private set; }

		public CorrelationResult(string ticker1, string ticker2, decimal coefficient1, decimal coefficient2, int observations)
		{
			Ticker1 = ticker1;
			Ticker2 = ticker2;
			Coefficient1 = coefficient1;
			Coefficient2 = coefficient2;
			Observations = observations;
		}
	}
}
