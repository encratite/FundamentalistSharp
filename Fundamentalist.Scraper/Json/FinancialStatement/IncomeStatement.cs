namespace Fundamentalist.Scraper.Json.FinancialStatement
{
	internal class IncomeStatement : DatedStatement
	{
		public Expense Expense { get; set; }
		public Income Income { get; set; }
		public Revenue Revenue { get; set; }
		public Cash Cash { get; set; }
		public string Currency { get; set; }
		public int FiscalYearEndMonth { get; set; }
	}
}
