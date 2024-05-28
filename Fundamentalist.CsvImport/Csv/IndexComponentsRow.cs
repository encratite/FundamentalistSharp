using CsvHelper.Configuration.Attributes;
using Fundamentalist.Common.Document;

namespace Fundamentalist.CsvImport.Csv
{
	internal class IndexComponentsRow
	{
		[Name("date")]
		public DateTime Date { get; set; }
		[Name("tickers")]
		public string Tickers { get; set; }

		public IndexComponents GetIndexComponents()
		{
			return new IndexComponents()
			{
				Date = Date,
				Tickers = Tickers.Split(",").ToList()
			};
		}
	}
}
