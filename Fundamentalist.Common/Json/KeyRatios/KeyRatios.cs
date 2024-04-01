namespace Fundamentalist.Common.Json.KeyRatios
{
	public class KeyRatios
	{
		public string StockId { get; set; }
		public string ExchangeId { get; set; }
		public string Market { get; set; }
		public string Industry { get; set; }
		public string DisplayName { get; set; }
		public string ShortName { get; set; }
		public string Symbol { get; set; }
		public List<CompanyMetric> CompanyMetrics { get; set; }
		public CompanyAverage3Years CompanyAverage3Years { get; set; }
		public CompanyAverage12Quarters CompanyAverage12Quarters { get; set; }
	}
}
