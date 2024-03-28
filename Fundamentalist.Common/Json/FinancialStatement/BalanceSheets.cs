namespace Fundamentalist.Common.Json.FinancialStatement
{
	public class BalanceSheets : DatedStatement
	{
		public CurrentAssets CurrentAssets { get; set; }
		public LongTermAssets LongTermAssets { get; set; }
		public CurrentLiabilities CurrentLiabilities { get; set; }
		public LongTermLiabilities LongTermLiabilities { get; set; }
		public Equity Equity { get; set; }
		public string Currency { get; set; }
		public int? FiscalYearEndMonth { get; set; }

		public float[] GetFeatures()
		{
			var currentAssetsFeatures = CurrentAssets.GetFeatures();
			var longTermAssetsFeatures = LongTermAssets.GetFeatures();
			var currentLiabilitiesFeatures = CurrentLiabilities.GetFeatures();
			var longTermLiabilitiesFeatures = LongTermLiabilities.GetFeatures();
			var equityFeatures = Equity.GetFeatures();
			var fiscalYearEndMonthFeatures = Features.Aggregate(FiscalYearEndMonth);
			var features = Features.Merge(
				currentAssetsFeatures,
				longTermAssetsFeatures,
				currentLiabilitiesFeatures,
				longTermLiabilitiesFeatures,
				equityFeatures,
				fiscalYearEndMonthFeatures
			);
			return features;
		}
	}
}
