using Fundamentalist.CsvGenerator.Csv;
using MongoDB.Bson.Serialization.Attributes;

namespace Fundamentalist.CsvGenerator.Document
{
	internal class SecNumber
	{
		public string Tag { get; set; }
		[BsonDateTimeOptions(DateOnly = true)]
		public DateTime EndDate { get; set; }
		public int Quarters { get; set; }
		public string Unit { get; set; }
		public decimal Value { get; set; }

		public SecNumber(NumberRow number)
		{
			Tag = number.Tag;
			EndDate = IntDate.Get(number.EndDate).Value;
			Quarters = number.Quarters;
			Unit = number.Unit;
			Value = number.Value ?? 0m;
		}
	}
}
