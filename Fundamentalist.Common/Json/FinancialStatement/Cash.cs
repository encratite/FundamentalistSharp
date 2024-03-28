namespace Fundamentalist.Common.Json.FinancialStatement
{
	public class Cash
	{
		public decimal? Investing { get; set; }
		public decimal? Financing { get; set; }
		public decimal? Total { get; set; }

		public float[] GetFeatures()
		{
			return Features.Aggregate(
				Investing,
				Financing,
				Total
			);
		}
	}
}
