namespace Fundamentalist.Common.Json.FinancialStatement
{
	public class Investing
	{
		public decimal? CashFromInvestingActivities { get; set; }
		public decimal? CapitalExpenditures { get; set; }
		public decimal? PurchaseOfFixedAssets { get; set; }
		public decimal? SaleMaturityOfInvestment { get; set; }
		public decimal? PurchaseOfInvestments { get; set; }
		public decimal? OtherInvestingCashFlow { get; set; }
		public decimal? OtherInvestingCashFlowItemsTotal { get; set; }

		public float[] GetFeatures()
		{
			return Features.Aggregate(
				CashFromInvestingActivities,
				CapitalExpenditures,
				PurchaseOfFixedAssets,
				SaleMaturityOfInvestment,
				PurchaseOfInvestments,
				OtherInvestingCashFlow,
				OtherInvestingCashFlowItemsTotal
			);
		}

		public static FeatureName[] GetFeatureNames(Investing @this)
		{
			return Features.AggregateNames(
				new FeatureName(nameof(Investing), nameof(CashFromInvestingActivities), @this?.CashFromInvestingActivities),
				new FeatureName(nameof(Investing), nameof(CapitalExpenditures), @this?.CapitalExpenditures),
				new FeatureName(nameof(Investing), nameof(PurchaseOfFixedAssets), @this?.PurchaseOfFixedAssets),
				new FeatureName(nameof(Investing), nameof(SaleMaturityOfInvestment), @this?.SaleMaturityOfInvestment),
				new FeatureName(nameof(Investing), nameof(PurchaseOfInvestments), @this?.PurchaseOfInvestments),
				new FeatureName(nameof(Investing), nameof(OtherInvestingCashFlow), @this?.OtherInvestingCashFlow),
				new FeatureName(nameof(Investing), nameof(OtherInvestingCashFlowItemsTotal), @this?.OtherInvestingCashFlowItemsTotal)
			);
		}
	}
}
