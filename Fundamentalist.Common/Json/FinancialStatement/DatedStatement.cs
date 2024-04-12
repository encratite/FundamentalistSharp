namespace Fundamentalist.Common.Json.FinancialStatement
{
	public class DatedStatement
	{
		public string Source { get; set; }
		public DateTime? SourceDate { get; set; }
		public DateTime? ReportDate { get; set; }
		public DateTime? EndDate { get; set; }

		public bool ValidDates()
		{
			return
				(!ReportDate.HasValue || ReportDate <= SourceDate) &&
				(!EndDate.HasValue || EndDate <= SourceDate);
		}

		public override string ToString()
		{
			if (Source != null && EndDate.HasValue)
				return $"{Source} {SourceDate.Value.ToShortDateString()}";
			else if (EndDate.HasValue)
				return $"(?) {EndDate.Value.ToShortDateString()}";
			else
				return "(?)";
		}
	}
}
