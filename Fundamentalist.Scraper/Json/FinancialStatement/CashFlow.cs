namespace Fundamentalist.Scraper.Json.FinancialStatement
{
	internal class CashFlow
	{
		public Financing Financing { get; set; }
		public Investing Investing { get; set; }
		public Operating Operating { get; set; }
		public string Currency { get; set; }
		public string Source { get; set; }
		public DateTime SourceDate { get; set; }
		public DateTime ReportDate { get; set; }
		public DateTime EndDate { get; set; }
		public int FiscalYearEndMonth { get; set; }

		public override string ToString()
		{
			return $"{Source} {SourceDate.ToShortDateString()}";
		}
	}
}
