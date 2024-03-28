namespace Fundamentalist.Common.Json.FinancialStatement
{
	public class FinancialStatement
	{
		public UnderlyingInstrument UnderlyingInstrument { get; set; }
		public BalanceSheets BalanceSheets { get; set; }
		public CashFlow CashFlow { get; set; }
		public IncomeStatement IncomeStatement { get; set; }
		public string StatementType { get; set; }
		public int Year { get; set; }
		public string Type { get; set; }
		public string _P { get; set; }
		public string Id { get; set; }
		public string _T { get; set; }

		public override string ToString()
		{
			return $"{UnderlyingInstrument.DisplayName} ({Type} {Year})";
		}

		public float[] GetFeatures()
		{
			var balanceSheetsFeatures = BalanceSheets.GetFeatures();
			var cashFlowFeatures = CashFlow.GetFeatures();
			var incomeStatementFeatures = IncomeStatement.GetFeatures();
			var features = Features.Merge(
				balanceSheetsFeatures,
				cashFlowFeatures,
				incomeStatementFeatures
			);
			return features;
		}
	}
}
