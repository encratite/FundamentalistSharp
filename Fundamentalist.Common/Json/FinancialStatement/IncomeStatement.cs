namespace Fundamentalist.Common.Json.FinancialStatement
{
	public class IncomeStatement : DatedStatement
	{
		public Expense Expense { get; set; }
		public Income Income { get; set; }
		public Revenue Revenue { get; set; }
		public Cash Cash { get; set; }
		public string Currency { get; set; }
		public int FiscalYearEndMonth { get; set; }

		public float[] GetFeatures()
		{
			var expenseFeatures = Expense.GetFeatures();
			var incomeFeatures = Income.GetFeatures();
			var revenueFeatures = Revenue.GetFeatures();
			var cashFeatures = Cash.GetFeatures();
			var features = Features.Merge(
				expenseFeatures,
				incomeFeatures,
				revenueFeatures,
				cashFeatures
			);
			return features;
		}

		public static FeatureName[] GetFeatureNames(IncomeStatement @this)
		{
			var expenseFeatures = Expense.GetFeatureNames(@this?.Expense);
			var incomeFeatures = Income.GetFeatureNames(@this?.Income);
			var revenueFeatures = Revenue.GetFeatureNames(@this?.Revenue);
			var cashFeatures = Cash.GetFeatureNames(@this?.Cash);
			var features = Features.MergeNames(
				expenseFeatures,
				incomeFeatures,
				revenueFeatures,
				cashFeatures
			);
			return features;
		}
	}
}
