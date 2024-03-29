namespace Fundamentalist.Common.Json.FinancialStatement
{
	public class Financing
	{
		public decimal? TotalCashDividendsPaid { get; set; }
		public decimal? IssuanceRetirementOfDebtNet { get; set; }
		public decimal? CashFromFinancingActivities { get; set; }
		public decimal? OtherFinancingCashFlow { get; set; }
		public decimal? FinancingCashFlowItems { get; set; }
		public decimal? CashDividendsPaidCommon { get; set; }
		public decimal? RepurchaseRetirementOfCommon { get; set; }
		public decimal? CommonStockNet { get; set; }
		public decimal? IssuanceRetirementOfStockNet { get; set; }
		public decimal? LongTermDebtIssued { get; set; }
		public decimal? LongTermDebtReduction { get; set; }
		public decimal? LongTermDebtNet { get; set; }

		public float[] GetFeatures()
		{
			return Features.Aggregate(
				TotalCashDividendsPaid,
				IssuanceRetirementOfDebtNet,
				CashFromFinancingActivities,
				OtherFinancingCashFlow,
				FinancingCashFlowItems,
				CashDividendsPaidCommon,
				RepurchaseRetirementOfCommon,
				CommonStockNet,
				IssuanceRetirementOfStockNet,
				LongTermDebtIssued,
				LongTermDebtReduction,
				LongTermDebtNet
			);
		}

		public static FeatureName[] GetFeatureNames(Financing @this)
		{
			return Features.AggregateNames(
				new FeatureName(nameof(Financing), nameof(TotalCashDividendsPaid), @this?.TotalCashDividendsPaid),
				new FeatureName(nameof(Financing), nameof(IssuanceRetirementOfDebtNet), @this?.IssuanceRetirementOfDebtNet),
				new FeatureName(nameof(Financing), nameof(CashFromFinancingActivities), @this?.CashFromFinancingActivities),
				new FeatureName(nameof(Financing), nameof(OtherFinancingCashFlow), @this?.OtherFinancingCashFlow),
				new FeatureName(nameof(Financing), nameof(FinancingCashFlowItems), @this?.FinancingCashFlowItems),
				new FeatureName(nameof(Financing), nameof(CashDividendsPaidCommon), @this?.CashDividendsPaidCommon),
				new FeatureName(nameof(Financing), nameof(RepurchaseRetirementOfCommon), @this?.RepurchaseRetirementOfCommon),
				new FeatureName(nameof(Financing), nameof(CommonStockNet), @this?.CommonStockNet),
				new FeatureName(nameof(Financing), nameof(IssuanceRetirementOfStockNet), @this?.IssuanceRetirementOfStockNet),
				new FeatureName(nameof(Financing), nameof(LongTermDebtIssued), @this?.LongTermDebtIssued),
				new FeatureName(nameof(Financing), nameof(LongTermDebtReduction), @this?.LongTermDebtReduction),
				new FeatureName(nameof(Financing), nameof(LongTermDebtNet), @this?.LongTermDebtNet)
			);
		}
	}
}
