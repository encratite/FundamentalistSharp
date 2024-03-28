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
	}
}
