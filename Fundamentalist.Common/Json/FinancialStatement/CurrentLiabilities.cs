namespace Fundamentalist.Common.Json.FinancialStatement
{
	public class CurrentLiabilities
	{
		public decimal? OtherCurrentLiabilitiesTotal { get; set; }
		public decimal? TotalCurrentLiabilities { get; set; }
		public decimal? LongTermDebt { get; set; }
		public decimal? CapitalLeaseObligations { get; set; }
		public decimal? TotalDebt { get; set; }
		public decimal? TotalLiabilities { get; set; }
		public decimal? NotesPayableShortTermDebt { get; set; }
		public decimal? CurrentPortOfLTDebtCapitalLeases { get; set; }
		public decimal? IncomeTaxesPayable { get; set; }
		public decimal? OtherCurrentLiabilities { get; set; }
		public decimal? TotalLongTermDebt { get; set; }
		public decimal? OtherLongTermLiabilities { get; set; }
		public decimal? OtherPayables { get; set; }
		public decimal? OtherLiabilitiesTotal { get; set; }
		public decimal? AccountsPayable { get; set; }

		public float[] GetFeatures()
		{
			return Features.Aggregate(
				OtherCurrentLiabilitiesTotal,
				TotalCurrentLiabilities,
				LongTermDebt,
				CapitalLeaseObligations,
				TotalDebt,
				TotalLiabilities,
				NotesPayableShortTermDebt,
				CurrentPortOfLTDebtCapitalLeases,
				IncomeTaxesPayable,
				OtherCurrentLiabilities,
				TotalLongTermDebt,
				OtherLongTermLiabilities,
				OtherPayables,
				OtherLiabilitiesTotal,
				AccountsPayable
			);
		}

		public static FeatureName[] GetFeatureNames(CurrentLiabilities @this)
		{
			return Features.AggregateNames(
				new FeatureName(nameof(CurrentLiabilities), nameof(OtherCurrentLiabilitiesTotal), @this?.OtherCurrentLiabilitiesTotal),
				new FeatureName(nameof(CurrentLiabilities), nameof(TotalCurrentLiabilities), @this?.TotalCurrentLiabilities),
				new FeatureName(nameof(CurrentLiabilities), nameof(LongTermDebt), @this?.LongTermDebt),
				new FeatureName(nameof(CurrentLiabilities), nameof(CapitalLeaseObligations), @this?.CapitalLeaseObligations),
				new FeatureName(nameof(CurrentLiabilities), nameof(TotalDebt), @this?.TotalDebt),
				new FeatureName(nameof(CurrentLiabilities), nameof(TotalLiabilities), @this?.TotalLiabilities),
				new FeatureName(nameof(CurrentLiabilities), nameof(NotesPayableShortTermDebt), @this?.NotesPayableShortTermDebt),
				new FeatureName(nameof(CurrentLiabilities), nameof(CurrentPortOfLTDebtCapitalLeases), @this?.CurrentPortOfLTDebtCapitalLeases),
				new FeatureName(nameof(CurrentLiabilities), nameof(IncomeTaxesPayable), @this?.IncomeTaxesPayable),
				new FeatureName(nameof(CurrentLiabilities), nameof(OtherCurrentLiabilities), @this?.OtherCurrentLiabilities),
				new FeatureName(nameof(CurrentLiabilities), nameof(TotalLongTermDebt), @this?.TotalLongTermDebt),
				new FeatureName(nameof(CurrentLiabilities), nameof(OtherLongTermLiabilities), @this?.OtherLongTermLiabilities),
				new FeatureName(nameof(CurrentLiabilities), nameof(OtherPayables), @this?.OtherPayables),
				new FeatureName(nameof(CurrentLiabilities), nameof(OtherLiabilitiesTotal), @this?.OtherLiabilitiesTotal),
				new FeatureName(nameof(CurrentLiabilities), nameof(AccountsPayable), @this?.AccountsPayable)
			);
		}
	}
}
