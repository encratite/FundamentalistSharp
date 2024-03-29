namespace Fundamentalist.Common.Json.FinancialStatement
{
	public class Expense
	{
		public decimal? GrossProfit { get; set; }
		public decimal? SellingGeneralAdminExpensesTotal { get; set; }
		public decimal? UnusualExpenseIncome { get; set; }
		public decimal? ResearchDevelopment { get; set; }
		public decimal? RestructuringCharge { get; set; }
		public decimal? InterestExpenseSupplemental { get; set; }
		public decimal? DepreciationSupplemental { get; set; }
		public decimal? AmortizationOfIntangiblesSupplemental { get; set; }
		public decimal? StockBasedCompensationSupplemental { get; set; }
		public decimal? RentalExpenseSupplemental { get; set; }
		public decimal? ResearchDevelopmentExpSupplemental { get; set; }
		public decimal? SellingGeneralAdminExpenses { get; set; }
		public decimal? LaborRelatedExpense { get; set; }
		public decimal? TotalOperatingExpense { get; set; }

		public float[] GetFeatures()
		{
			return Features.Aggregate(
				GrossProfit,
				SellingGeneralAdminExpensesTotal,
				UnusualExpenseIncome,
				ResearchDevelopment,
				RestructuringCharge,
				InterestExpenseSupplemental,
				DepreciationSupplemental,
				AmortizationOfIntangiblesSupplemental,
				StockBasedCompensationSupplemental,
				RentalExpenseSupplemental,
				ResearchDevelopmentExpSupplemental,
				SellingGeneralAdminExpenses,
				LaborRelatedExpense,
				TotalOperatingExpense
			);
		}

		public static FeatureName[] GetFeatureNames(Expense @this)
		{
			return Features.AggregateNames(
				new FeatureName(nameof(Expense), nameof(GrossProfit), @this?.GrossProfit),
				new FeatureName(nameof(Expense), nameof(SellingGeneralAdminExpensesTotal), @this?.SellingGeneralAdminExpensesTotal),
				new FeatureName(nameof(Expense), nameof(UnusualExpenseIncome), @this?.UnusualExpenseIncome),
				new FeatureName(nameof(Expense), nameof(ResearchDevelopment), @this?.ResearchDevelopment),
				new FeatureName(nameof(Expense), nameof(RestructuringCharge), @this?.RestructuringCharge),
				new FeatureName(nameof(Expense), nameof(InterestExpenseSupplemental), @this?.InterestExpenseSupplemental),
				new FeatureName(nameof(Expense), nameof(DepreciationSupplemental), @this?.DepreciationSupplemental),
				new FeatureName(nameof(Expense), nameof(AmortizationOfIntangiblesSupplemental), @this?.AmortizationOfIntangiblesSupplemental),
				new FeatureName(nameof(Expense), nameof(StockBasedCompensationSupplemental), @this?.StockBasedCompensationSupplemental),
				new FeatureName(nameof(Expense), nameof(RentalExpenseSupplemental), @this?.RentalExpenseSupplemental),
				new FeatureName(nameof(Expense), nameof(ResearchDevelopmentExpSupplemental), @this?.ResearchDevelopmentExpSupplemental),
				new FeatureName(nameof(Expense), nameof(SellingGeneralAdminExpenses), @this?.SellingGeneralAdminExpenses),
				new FeatureName(nameof(Expense), nameof(LaborRelatedExpense), @this?.LaborRelatedExpense),
				new FeatureName(nameof(Expense), nameof(TotalOperatingExpense), @this?.TotalOperatingExpense)
			);
		}
	}
}
