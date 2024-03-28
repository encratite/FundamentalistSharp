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
	}
}
