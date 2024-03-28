﻿namespace Fundamentalist.Common.Json.FinancialStatement
{
	public class Investing
	{
		public decimal? CashFromInvestingActivities { get; set; }
		public decimal? CapitalExpenditures { get; set; }
		public decimal? PurchaseOfFixedAssets { get; set; }
		public decimal? SaleMaturityOfInvestment { get; set; }
		public decimal? PurchaseOfInvestments { get; set; }
		public decimal? OtherInvestingCashFlow { get; set; }
		public decimal? OtherInvestingCashFlowItemsTotal { get; set; }

		public float[] GetFeatures()
		{
			return Features.Aggregate(
				CashFromInvestingActivities,
				CapitalExpenditures,
				PurchaseOfFixedAssets,
				SaleMaturityOfInvestment,
				PurchaseOfInvestments,
				OtherInvestingCashFlow,
				OtherInvestingCashFlowItemsTotal
			);
		}
	}
}
