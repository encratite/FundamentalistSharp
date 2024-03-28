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
	}
}
