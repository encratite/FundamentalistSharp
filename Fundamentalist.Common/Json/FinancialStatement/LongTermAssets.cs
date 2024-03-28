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
	}
}
