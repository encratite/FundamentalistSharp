namespace Fundamentalist.Common.Json.FinancialStatement
{
	public class LongTermLiabilities
	{
		public decimal? LongTermDebtMaturingWithin1Year { get; set; }
		public decimal? LongTermDebtMaturingInYear2 { get; set; }
		public decimal? LongTermDebtMaturingInYear3 { get; set; }
		public decimal? LongTermDebtMaturingInYear4 { get; set; }
		public decimal? LongTermDebtMaturingInYear5 { get; set; }
		public decimal? LongTermDebtMaturingIn2Or3Years { get; set; }
		public decimal? LongTermDebtMaturingIn4Or5Years { get; set; }
		public decimal? LongTermDebtMaturingInYear6AndBeyond { get; set; }
		public decimal? TotalLongTermDebtSupplemental { get; set; }
		public decimal? OperatingLeasePaymentsDueInYear1 { get; set; }
		public decimal? OperatingLeasePaymentsDueInYear2 { get; set; }
		public decimal? OperatingLeasePaymentsDueInYear3 { get; set; }
		public decimal? OperatingLeasePaymentsDueInYear4 { get; set; }
		public decimal? OperatingLeasePaymentsDueInYear5 { get; set; }
		public decimal? OperatingLeasePymtsDuein2Or3Years { get; set; }
		public decimal? OperatingLeasePymtsDuein45Years { get; set; }
		public decimal? OperatingLeasePaymentsDueInYear6AndBeyond { get; set; }

		public float[] GetFeatures()
		{
			return Features.Aggregate(
				LongTermDebtMaturingWithin1Year,
				LongTermDebtMaturingInYear2,
				LongTermDebtMaturingInYear3,
				LongTermDebtMaturingInYear4,
				LongTermDebtMaturingInYear5,
				LongTermDebtMaturingIn2Or3Years,
				LongTermDebtMaturingIn4Or5Years,
				LongTermDebtMaturingInYear6AndBeyond,
				TotalLongTermDebtSupplemental,
				OperatingLeasePaymentsDueInYear1,
				OperatingLeasePaymentsDueInYear2,
				OperatingLeasePaymentsDueInYear3,
				OperatingLeasePaymentsDueInYear4,
				OperatingLeasePaymentsDueInYear5,
				OperatingLeasePymtsDuein2Or3Years,
				OperatingLeasePymtsDuein45Years,
				OperatingLeasePaymentsDueInYear6AndBeyond
			);
		}

		public static FeatureName[] GetFeatureNames(LongTermLiabilities @this)
		{
			return Features.AggregateNames(
				new FeatureName(nameof(LongTermLiabilities), nameof(LongTermDebtMaturingWithin1Year), @this?.LongTermDebtMaturingWithin1Year),
				new FeatureName(nameof(LongTermLiabilities), nameof(LongTermDebtMaturingInYear2), @this?.LongTermDebtMaturingInYear2),
				new FeatureName(nameof(LongTermLiabilities), nameof(LongTermDebtMaturingInYear3), @this?.LongTermDebtMaturingInYear3),
				new FeatureName(nameof(LongTermLiabilities), nameof(LongTermDebtMaturingInYear4), @this?.LongTermDebtMaturingInYear4),
				new FeatureName(nameof(LongTermLiabilities), nameof(LongTermDebtMaturingInYear5), @this?.LongTermDebtMaturingInYear5),
				new FeatureName(nameof(LongTermLiabilities), nameof(LongTermDebtMaturingIn2Or3Years), @this?.LongTermDebtMaturingIn2Or3Years),
				new FeatureName(nameof(LongTermLiabilities), nameof(LongTermDebtMaturingIn4Or5Years), @this?.LongTermDebtMaturingIn4Or5Years),
				new FeatureName(nameof(LongTermLiabilities), nameof(LongTermDebtMaturingInYear6AndBeyond), @this?.LongTermDebtMaturingInYear6AndBeyond),
				new FeatureName(nameof(LongTermLiabilities), nameof(TotalLongTermDebtSupplemental), @this?.TotalLongTermDebtSupplemental),
				new FeatureName(nameof(LongTermLiabilities), nameof(OperatingLeasePaymentsDueInYear1), @this?.OperatingLeasePaymentsDueInYear1),
				new FeatureName(nameof(LongTermLiabilities), nameof(OperatingLeasePaymentsDueInYear2), @this?.OperatingLeasePaymentsDueInYear2),
				new FeatureName(nameof(LongTermLiabilities), nameof(OperatingLeasePaymentsDueInYear3), @this?.OperatingLeasePaymentsDueInYear3),
				new FeatureName(nameof(LongTermLiabilities), nameof(OperatingLeasePaymentsDueInYear4), @this?.OperatingLeasePaymentsDueInYear4),
				new FeatureName(nameof(LongTermLiabilities), nameof(OperatingLeasePaymentsDueInYear5), @this?.OperatingLeasePaymentsDueInYear5),
				new FeatureName(nameof(LongTermLiabilities), nameof(OperatingLeasePymtsDuein2Or3Years), @this?.OperatingLeasePymtsDuein2Or3Years),
				new FeatureName(nameof(LongTermLiabilities), nameof(OperatingLeasePymtsDuein45Years), @this?.OperatingLeasePymtsDuein45Years),
				new FeatureName(nameof(LongTermLiabilities), nameof(OperatingLeasePaymentsDueInYear6AndBeyond), @this?.OperatingLeasePaymentsDueInYear6AndBeyond)
			);
		}
	}
}
