using Fundamentalist.Common;
using Microsoft.ML.Data;

namespace Fundamentalist.Trainer
{
	public class DataPoint
	{
		public float[] Features { get; set; }

		public float Label { get; set; }

		[NoColumn]
		public string Ticker { get; set; }

		[NoColumn]
		public DateTime Date { get; set; }

		[NoColumn]
		public SortedList<DateTime, PriceData> PriceData { get; set; }

		[NoColumn]
		public float? Score { get; set; }
	}
}
