namespace Fundamentalist.Common.Json.FinancialStatement
{
	public class Income
	{
		public decimal? IncomeAvailableToComExclExtraOrd { get; set; }
		public decimal? IncomeAvailableToComInclExtraOrd { get; set; }
		public decimal? ProvisionForIncomeTaxes { get; set; }
		public decimal? NetIncomeBeforeTaxes { get; set; }
		public decimal? OtherNet { get; set; }
		public decimal? NetIncomeBeforeExtraItems { get; set; }
		public decimal? MinorityInterest { get; set; }
		public decimal? OperatingIncome { get; set; }
		public decimal? InterestIncExpNetNonOpTotal { get; set; }
		public decimal? NetInterestIncome { get; set; }
		public decimal? InterestInvestIncomeNonOperating { get; set; }
		public decimal? OtherNonOperatingIncomeExpense { get; set; }
		public decimal? InterestExpenseNonOperating { get; set; }
		public decimal? IncomeInclExtraBeforeDistributions { get; set; }
		public decimal? NormalizedIncomeBeforeTaxes { get; set; }
		public decimal? IncomeTaxExImpactOfSpeciaItems { get; set; }
		public decimal? NormalizedIncomeAfterTaxes { get; set; }
		public decimal? NormalizedIncAvailToCom { get; set; }
		public decimal? NetIncomeAfterTaxes { get; set; }
		public decimal? InvestmentIncomeNonOperating { get; set; }
		public decimal? EffectOfSpecialItemsOnIncomeTaxes { get; set; }
		public decimal? InterestExpenseNetNonOperating { get; set; }
		public decimal? BankTotalRevenue { get; set; }

		public float[] GetFeatures()
		{
			return Features.Aggregate(
				IncomeAvailableToComExclExtraOrd,
				IncomeAvailableToComInclExtraOrd,
				ProvisionForIncomeTaxes,
				NetIncomeBeforeTaxes,
				OtherNet,
				NetIncomeBeforeExtraItems,
				MinorityInterest,
				OperatingIncome,
				InterestIncExpNetNonOpTotal,
				NetInterestIncome,
				InterestInvestIncomeNonOperating,
				OtherNonOperatingIncomeExpense,
				InterestExpenseNonOperating,
				IncomeInclExtraBeforeDistributions,
				NormalizedIncomeBeforeTaxes,
				IncomeTaxExImpactOfSpeciaItems,
				NormalizedIncomeAfterTaxes,
				NormalizedIncAvailToCom,
				NetIncomeAfterTaxes,
				InvestmentIncomeNonOperating,
				EffectOfSpecialItemsOnIncomeTaxes,
				InterestExpenseNetNonOperating,
				BankTotalRevenue
			);
		}
	}
}
