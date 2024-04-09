using Fundamentalist.Common;
using Microsoft.ML.Data;

namespace Fundamentalist.Trainer
{
	public class DataPoint
	{
		public string Ticker { get; set; }
		public float[] Features { get; set; }
		public float Label { get; set; }
		public DateTime Date { get; set; }
		[NoColumn]
		public List<PriceData> PriceData { get; set; }
	}
}
