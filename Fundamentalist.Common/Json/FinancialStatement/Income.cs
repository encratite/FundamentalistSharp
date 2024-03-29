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

		public static FeatureName[] GetFeatureNames(Income @this)
		{
			return Features.AggregateNames(
				new FeatureName(nameof(Income), nameof(IncomeAvailableToComExclExtraOrd), @this?.IncomeAvailableToComExclExtraOrd),
				new FeatureName(nameof(Income), nameof(IncomeAvailableToComInclExtraOrd), @this?.IncomeAvailableToComInclExtraOrd),
				new FeatureName(nameof(Income), nameof(ProvisionForIncomeTaxes), @this?.ProvisionForIncomeTaxes),
				new FeatureName(nameof(Income), nameof(NetIncomeBeforeTaxes), @this?.NetIncomeBeforeTaxes),
				new FeatureName(nameof(Income), nameof(OtherNet), @this?.OtherNet),
				new FeatureName(nameof(Income), nameof(NetIncomeBeforeExtraItems), @this?.NetIncomeBeforeExtraItems),
				new FeatureName(nameof(Income), nameof(MinorityInterest), @this?.MinorityInterest),
				new FeatureName(nameof(Income), nameof(OperatingIncome), @this?.OperatingIncome),
				new FeatureName(nameof(Income), nameof(InterestIncExpNetNonOpTotal), @this?.InterestIncExpNetNonOpTotal),
				new FeatureName(nameof(Income), nameof(NetInterestIncome), @this?.NetInterestIncome),
				new FeatureName(nameof(Income), nameof(InterestInvestIncomeNonOperating), @this?.InterestInvestIncomeNonOperating),
				new FeatureName(nameof(Income), nameof(OtherNonOperatingIncomeExpense), @this?.OtherNonOperatingIncomeExpense),
				new FeatureName(nameof(Income), nameof(InterestExpenseNonOperating), @this?.InterestExpenseNonOperating),
				new FeatureName(nameof(Income), nameof(IncomeInclExtraBeforeDistributions), @this?.IncomeInclExtraBeforeDistributions),
				new FeatureName(nameof(Income), nameof(NormalizedIncomeBeforeTaxes), @this?.NormalizedIncomeBeforeTaxes),
				new FeatureName(nameof(Income), nameof(IncomeTaxExImpactOfSpeciaItems), @this?.IncomeTaxExImpactOfSpeciaItems),
				new FeatureName(nameof(Income), nameof(NormalizedIncomeAfterTaxes), @this?.NormalizedIncomeAfterTaxes),
				new FeatureName(nameof(Income), nameof(NormalizedIncAvailToCom), @this?.NormalizedIncAvailToCom),
				new FeatureName(nameof(Income), nameof(NetIncomeAfterTaxes), @this?.NetIncomeAfterTaxes),
				new FeatureName(nameof(Income), nameof(InvestmentIncomeNonOperating), @this?.InvestmentIncomeNonOperating),
				new FeatureName(nameof(Income), nameof(EffectOfSpecialItemsOnIncomeTaxes), @this?.EffectOfSpecialItemsOnIncomeTaxes),
				new FeatureName(nameof(Income), nameof(InterestExpenseNetNonOperating), @this?.InterestExpenseNetNonOperating),
				new FeatureName(nameof(Income), nameof(BankTotalRevenue), @this?.BankTotalRevenue)
			);
		}
	}
}
