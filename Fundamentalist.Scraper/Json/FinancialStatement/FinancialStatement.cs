namespace Fundamentalist.Scraper.Json.FinancialStatement
{
	internal class FinancialStatement
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
	}
}
