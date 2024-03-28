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
		public int FiscalYearEndMonth { get; set; }
	}
}
