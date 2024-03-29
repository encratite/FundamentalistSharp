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

		public static FeatureName[] GetFeatureNames(Cash @this)
		{
			return Features.AggregateNames(
				new FeatureName(nameof(Cash), nameof(Investing), @this?.Investing),
				new FeatureName(nameof(Cash), nameof(Financing), @this?.Financing),
				new FeatureName(nameof(Cash), nameof(Total), @this?.Total)
			);
		}
	}
}
