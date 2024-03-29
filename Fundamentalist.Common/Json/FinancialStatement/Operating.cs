namespace Fundamentalist.Common.Json.FinancialStatement
{
	public class Operating
	{
		public decimal? NetIncomeStartingLine { get; set; }
		public decimal? DepreciationDepletion { get; set; }
		public decimal? ChangesInWorkingCapital { get; set; }
		public decimal? CashFromOperatingActivities { get; set; }
		public decimal? NetChangeInCash { get; set; }
		public decimal? ForeignExchangeEffects { get; set; }
		public decimal? OtherNonCashItems { get; set; }
		public decimal? NonCashItems { get; set; }
		public decimal? AccountsReceivable { get; set; }
		public decimal? Inventories { get; set; }
		public decimal? OtherLiabilities { get; set; }
		public decimal? DepreciationSupplemental { get; set; }
		public decimal? NetCashBeginningBalance { get; set; }
		public decimal? NetCashEndingBalance { get; set; }
		public decimal? PayableAccrued { get; set; }

		public float[] GetFeatures()
		{
			return Features.Aggregate(
				NetIncomeStartingLine,
				DepreciationDepletion,
				ChangesInWorkingCapital,
				CashFromOperatingActivities,
				NetChangeInCash,
				ForeignExchangeEffects,
				OtherNonCashItems,
				NonCashItems,
				AccountsReceivable,
				Inventories,
				OtherLiabilities,
				DepreciationSupplemental,
				NetCashBeginningBalance,
				NetCashEndingBalance,
				PayableAccrued
			);
		}

		public static FeatureName[] GetFeatureNames(Operating @this)
		{
			return Features.AggregateNames(
				new FeatureName(nameof(Operating), nameof(NetIncomeStartingLine), @this?.NetIncomeStartingLine),
				new FeatureName(nameof(Operating), nameof(DepreciationDepletion), @this?.DepreciationDepletion),
				new FeatureName(nameof(Operating), nameof(ChangesInWorkingCapital), @this?.ChangesInWorkingCapital),
				new FeatureName(nameof(Operating), nameof(CashFromOperatingActivities), @this?.CashFromOperatingActivities),
				new FeatureName(nameof(Operating), nameof(NetChangeInCash), @this?.NetChangeInCash),
				new FeatureName(nameof(Operating), nameof(ForeignExchangeEffects), @this?.ForeignExchangeEffects),
				new FeatureName(nameof(Operating), nameof(OtherNonCashItems), @this?.OtherNonCashItems),
				new FeatureName(nameof(Operating), nameof(NonCashItems), @this?.NonCashItems),
				new FeatureName(nameof(Operating), nameof(AccountsReceivable), @this?.AccountsReceivable),
				new FeatureName(nameof(Operating), nameof(Inventories), @this?.Inventories),
				new FeatureName(nameof(Operating), nameof(OtherLiabilities), @this?.OtherLiabilities),
				new FeatureName(nameof(Operating), nameof(DepreciationSupplemental), @this?.DepreciationSupplemental),
				new FeatureName(nameof(Operating), nameof(NetCashBeginningBalance), @this?.NetCashBeginningBalance),
				new FeatureName(nameof(Operating), nameof(NetCashEndingBalance), @this?.NetCashEndingBalance),
				new FeatureName(nameof(Operating), nameof(PayableAccrued), @this?.PayableAccrued)
			);
		}
	}
}
