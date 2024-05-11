using Microsoft.ML.Data;

namespace Fundamentalist.Trainer
{
	internal enum PerformanceLabelType : UInt32
	{
		Underperform,
		Neutral,
		Outperform
	}

	internal class DataPoint
	{
		public float[] Features { get; set; }

		[KeyType(3)]
		public UInt32 Label { get; set; }

		[NoColumn]
		public string Ticker { get; set; }

		[NoColumn]
		public DateOnly Date { get; set; }

		[NoColumn]
		public decimal Performance { get; set; }
	}
}
