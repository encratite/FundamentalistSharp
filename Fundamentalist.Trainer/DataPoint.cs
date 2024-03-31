using Microsoft.ML.Data;

namespace Fundamentalist.Trainer
{
	internal class DataPoint
	{
		[VectorType(885)]
		public float[] Features { get; set; }
		public bool Label { get; set; }
	}
}
