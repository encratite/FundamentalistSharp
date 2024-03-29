namespace Fundamentalist.Common.Json.FinancialStatement
{
	public class LongTermAssets
	{
		public decimal? TotalOperatingLeasesSupplemental { get; set; }
		public decimal? TotalCurrentAssetsLessInventory { get; set; }
		public decimal? QuickRatio { get; set; }
		public decimal? CurrentRatio { get; set; }
		public decimal? NetDebtInclPrefStockMinInterest { get; set; }
		public decimal? TangibleBookValueCommonEquity { get; set; }
		public decimal? TangibleBookValuePerShareCommonEq { get; set; }

		public float[] GetFeatures()
		{
			return Features.Aggregate(
				TotalOperatingLeasesSupplemental,
				TotalCurrentAssetsLessInventory,
				QuickRatio,
				CurrentRatio,
				NetDebtInclPrefStockMinInterest,
				TangibleBookValueCommonEquity,
				TangibleBookValuePerShareCommonEq
			);
		}

		public static FeatureName[] GetFeatureNames(LongTermAssets @this)
		{
			return Features.AggregateNames(
				new FeatureName(nameof(LongTermAssets), nameof(TotalOperatingLeasesSupplemental), @this?.TotalOperatingLeasesSupplemental),
				new FeatureName(nameof(LongTermAssets), nameof(TotalCurrentAssetsLessInventory), @this?.TotalCurrentAssetsLessInventory),
				new FeatureName(nameof(LongTermAssets), nameof(QuickRatio), @this?.QuickRatio),
				new FeatureName(nameof(LongTermAssets), nameof(CurrentRatio), @this?.CurrentRatio),
				new FeatureName(nameof(LongTermAssets), nameof(NetDebtInclPrefStockMinInterest), @this?.NetDebtInclPrefStockMinInterest),
				new FeatureName(nameof(LongTermAssets), nameof(TangibleBookValueCommonEquity), @this?.TangibleBookValueCommonEquity),
				new FeatureName(nameof(LongTermAssets), nameof(TangibleBookValuePerShareCommonEq), @this?.TangibleBookValuePerShareCommonEq)
			);
		}
	}
}
