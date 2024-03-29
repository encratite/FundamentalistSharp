﻿namespace Fundamentalist.Common.Json.FinancialStatement
{
	public class CashFlow : DatedStatement
	{
		public Financing Financing { get; set; }
		public Investing Investing { get; set; }
		public Operating Operating { get; set; }
		public string Currency { get; set; }
		public int FiscalYearEndMonth { get; set; }
	}
}
