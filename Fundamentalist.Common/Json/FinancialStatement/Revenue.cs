namespace Fundamentalist.Common.Json.FinancialStatement
{
	public class Revenue
	{
		public decimal? DpsCommonStockPrimaryIssue { get; set; }
		public decimal? CostOfRevenueTotal { get; set; }
		public decimal? NetSales { get; set; }
		public decimal? CostOfRevenue { get; set; }
		public decimal? BasicNormalizedEPS { get; set; }
		public decimal? DilutedNormalizedEPS { get; set; }
		public decimal? GrossMargin { get; set; }
		public decimal? OperatingMargin { get; set; }
		public decimal? NormalizedEBIT { get; set; }
		public decimal? DilutedWeightedAverageShares { get; set; }
		public decimal? DilutedEPSExcludingExtraOrdItems { get; set; }
		public decimal? BasicWeightedAverageShares { get; set; }
		public decimal? BasicEPSExcludingExtraordinaryItems { get; set; }
		public decimal? BasicEPSIncludingExtraordinaryItems { get; set; }
		public decimal? NormalizedEBITDA { get; set; }
		public decimal? DilutedEPSIncludingExtraOrdItems { get; set; }
		public decimal? TotalRevenue { get; set; }

		public float[] GetFeatures()
		{
			return Features.Aggregate(
				DpsCommonStockPrimaryIssue,
				CostOfRevenueTotal,
				NetSales,
				CostOfRevenue,
				BasicNormalizedEPS,
				DilutedNormalizedEPS,
				GrossMargin,
				OperatingMargin,
				NormalizedEBIT,
				DilutedWeightedAverageShares,
				DilutedEPSExcludingExtraOrdItems,
				BasicWeightedAverageShares,
				BasicEPSExcludingExtraordinaryItems,
				BasicEPSIncludingExtraordinaryItems,
				NormalizedEBITDA,
				DilutedEPSIncludingExtraOrdItems,
				TotalRevenue
			);
		}

		public static FeatureName[] GetFeatureNames(Revenue @this)
		{
			return Features.AggregateNames(
				new FeatureName(nameof(Revenue), nameof(DpsCommonStockPrimaryIssue), @this?.DpsCommonStockPrimaryIssue),
				new FeatureName(nameof(Revenue), nameof(CostOfRevenueTotal), @this?.CostOfRevenueTotal),
				new FeatureName(nameof(Revenue), nameof(NetSales), @this?.NetSales),
				new FeatureName(nameof(Revenue), nameof(CostOfRevenue), @this?.CostOfRevenue),
				new FeatureName(nameof(Revenue), nameof(BasicNormalizedEPS), @this?.BasicNormalizedEPS),
				new FeatureName(nameof(Revenue), nameof(DilutedNormalizedEPS), @this?.DilutedNormalizedEPS),
				new FeatureName(nameof(Revenue), nameof(GrossMargin), @this?.GrossMargin),
				new FeatureName(nameof(Revenue), nameof(OperatingMargin), @this?.OperatingMargin),
				new FeatureName(nameof(Revenue), nameof(NormalizedEBIT), @this?.NormalizedEBIT),
				new FeatureName(nameof(Revenue), nameof(DilutedWeightedAverageShares), @this?.DilutedWeightedAverageShares),
				new FeatureName(nameof(Revenue), nameof(DilutedEPSExcludingExtraOrdItems), @this?.DilutedEPSExcludingExtraOrdItems),
				new FeatureName(nameof(Revenue), nameof(BasicWeightedAverageShares), @this?.BasicWeightedAverageShares),
				new FeatureName(nameof(Revenue), nameof(BasicEPSExcludingExtraordinaryItems), @this?.BasicEPSExcludingExtraordinaryItems),
				new FeatureName(nameof(Revenue), nameof(BasicEPSIncludingExtraordinaryItems), @this?.BasicEPSIncludingExtraordinaryItems),
				new FeatureName(nameof(Revenue), nameof(NormalizedEBITDA), @this?.NormalizedEBITDA),
				new FeatureName(nameof(Revenue), nameof(DilutedEPSIncludingExtraOrdItems), @this?.DilutedEPSIncludingExtraOrdItems),
				new FeatureName(nameof(Revenue), nameof(TotalRevenue), @this?.TotalRevenue)
			);
		}
	}
}
