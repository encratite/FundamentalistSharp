namespace Fundamentalist.Common.Json.FinancialStatement
{
	public class Equity
	{
		public decimal? CommonStockTotal { get; set; }
		public decimal? AdditionalPaidInCapital { get; set; }
		public decimal? RetainedEarningsAccumulatedDeficit { get; set; }
		public decimal? UnrealizedGainLoss { get; set; }
		public decimal? TotalEquity { get; set; }
		public decimal? TotalLiabilitiesShareholdersEquity { get; set; }
		public decimal? TotalCommonSharesOutstanding { get; set; }
		public decimal? CommonStock { get; set; }
		public decimal? OtherComprehensiveIncome { get; set; }
		public decimal? OtherEquityTotal { get; set; }
		public decimal? TotalEquityMinorityInterest { get; set; }
		public decimal? SharesOutstandingCommonStockPrimaryIssue { get; set; }
		public decimal? TreasurySharesCommonStockPrimaryIssue { get; set; }
		public decimal? AccumulatedIntangibleAmortSuppl { get; set; }
		public decimal? TranslationAdjustment { get; set; }

		public float[] GetFeatures()
		{
			return Features.Aggregate(
				CommonStockTotal,
				AdditionalPaidInCapital,
				RetainedEarningsAccumulatedDeficit,
				UnrealizedGainLoss,
				TotalEquity,
				TotalLiabilitiesShareholdersEquity,
				TotalCommonSharesOutstanding,
				CommonStock,
				OtherComprehensiveIncome,
				OtherEquityTotal,
				TotalEquityMinorityInterest,
				SharesOutstandingCommonStockPrimaryIssue,
				TreasurySharesCommonStockPrimaryIssue,
				AccumulatedIntangibleAmortSuppl,
				TranslationAdjustment
			);
		}

		public static FeatureName[] GetFeatureNames(Equity @this)
		{
			return Features.AggregateNames(
				new FeatureName(nameof(Equity), nameof(CommonStockTotal), @this?.CommonStockTotal),
				new FeatureName(nameof(Equity), nameof(AdditionalPaidInCapital), @this?.AdditionalPaidInCapital),
				new FeatureName(nameof(Equity), nameof(RetainedEarningsAccumulatedDeficit), @this?.RetainedEarningsAccumulatedDeficit),
				new FeatureName(nameof(Equity), nameof(UnrealizedGainLoss), @this?.UnrealizedGainLoss),
				new FeatureName(nameof(Equity), nameof(TotalEquity), @this?.TotalEquity),
				new FeatureName(nameof(Equity), nameof(TotalLiabilitiesShareholdersEquity), @this?.TotalLiabilitiesShareholdersEquity),
				new FeatureName(nameof(Equity), nameof(TotalCommonSharesOutstanding), @this?.TotalCommonSharesOutstanding),
				new FeatureName(nameof(Equity), nameof(CommonStock), @this?.CommonStock),
				new FeatureName(nameof(Equity), nameof(OtherComprehensiveIncome), @this?.OtherComprehensiveIncome),
				new FeatureName(nameof(Equity), nameof(OtherEquityTotal), @this?.OtherEquityTotal),
				new FeatureName(nameof(Equity), nameof(TotalEquityMinorityInterest), @this?.TotalEquityMinorityInterest),
				new FeatureName(nameof(Equity), nameof(SharesOutstandingCommonStockPrimaryIssue), @this?.SharesOutstandingCommonStockPrimaryIssue),
				new FeatureName(nameof(Equity), nameof(TreasurySharesCommonStockPrimaryIssue), @this?.TreasurySharesCommonStockPrimaryIssue),
				new FeatureName(nameof(Equity), nameof(AccumulatedIntangibleAmortSuppl), @this?.AccumulatedIntangibleAmortSuppl),
				new FeatureName(nameof(Equity), nameof(TranslationAdjustment), @this?.TranslationAdjustment)
			);
		}
	}
}
