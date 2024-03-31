using Microsoft.ML.Data;

namespace Fundamentalist.Trainer
{
	internal class DataPoint
	{
		public float[] Features { get; set; }
		public bool Label { get; set; }
	}
}
