namespace Fundamentalist.Common
{
	public class PriceData
	{
		public DateTime? Date { get; set; }
		public decimal? Open { get; set; }
		public decimal? High { get; set; }
		public decimal? Low { get; set; }
		public decimal? Close { get; set; }
		public decimal? AdjustedClose { get; set; }
		public long? Volume { get; set; }

		public decimal? Mean
		{
			get => (Open + Close) / 2.0m;
		}

		public bool HasInvalidValues()
		{
			return
				Date == null ||
				Open == null ||
				High == null ||
				Low == null ||
				Close == null ||
				AdjustedClose == null ||
				Volume == null ||
				Volume.Value == 0;
		}

		public override string ToString()
		{
			return $"{Date.Value.ToShortDateString()} {Open.Value}";
		}
	}
}
