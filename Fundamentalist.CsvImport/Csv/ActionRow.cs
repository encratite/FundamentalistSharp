using CsvHelper.Configuration.Attributes;
using Fundamentalist.Common.Document;

namespace Fundamentalist.CsvImport.Csv
{
	internal class ActionRow
	{
		[Name("date")]
		public DateTime Date { get; set; }
		[Name("action")]
		public string Action { get; set; }
		[Name("ticker")]
		public string Ticker { get; set; }
		[Name("name")]
		public string Name { get; set; }
		[Name("value")]
		public decimal? Value { get; set; }
		[Name("contraticker")]
		public string ContraTicker { get; set; }
		[Name("contraname")]
		public string ContraName { get; set; }

		public CorporateAction GetCorporateAction()
		{
			return new CorporateAction
			{
				Date = Date,
				Action = Action,
				Ticker = Ticker,
				Name = Name,
				Value = Value,
				ContraTicker = ContraTicker,
				ContraName = ContraName,
			};
		}
	}
}
