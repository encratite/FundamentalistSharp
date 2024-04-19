using Microsoft.ML.Data;

namespace Fundamentalist.Trainer
{
	public class DataPoint
	{
		// public float[] Features { get; set; } = new float[] { };
		public float[] CloseOpenFeatures { get; set; }
		public float[] HighLowFeatures { get; set; }
		public float[] VolumeFeatures { get; set; }

		public bool Label { get; set; }

		[NoColumn]
		public bool[] Labels { get; set; }

		[NoColumn]
		public decimal[] PerformanceRatios { get; set; }

		[NoColumn]
		public DateTime Date { get; set; }

		[NoColumn]
		public bool? PredictedLabel { get; set; }
	}
}
