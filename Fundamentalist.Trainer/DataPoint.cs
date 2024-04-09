using Fundamentalist.Common;
using Microsoft.ML.Data;

namespace Fundamentalist.Trainer
{
	public class DataPoint
	{
		[NoColumn]
		public string Ticker { get; set; }

		public float[] Features { get; set; }

		public float Label { get; set; }

		[NoColumn]
		public DateTime Date { get; set; }

		[NoColumn]
		public List<PriceData> PriceData { get; set; }

		public float Score { get; set; }
	}
}
