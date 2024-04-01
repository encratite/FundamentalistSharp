using System.Text.Json.Serialization;

namespace Fundamentalist.Common.Json.KeyRatios
{
	public class CompanyAverage12Quarters
	{
		[JsonConverter(typeof(NaNJsonConverter))]
		public decimal? RevenuePerShare { get; set; }
		[JsonConverter(typeof(NaNJsonConverter))]
		public decimal? EarningsPerShare { get; set; }
		[JsonConverter(typeof(NaNJsonConverter))]
		public decimal? FreeCashFlowPerShare { get; set; }
		[JsonConverter(typeof(NaNJsonConverter))]
		public decimal? BookValuePerShare { get; set; }
		[JsonConverter(typeof(NaNJsonConverter))]
		public decimal? RevenueGrowthRate { get; set; }
		[JsonConverter(typeof(NaNJsonConverter))]
		public decimal? EarningsGrowthRate { get; set; }
		[JsonConverter(typeof(NaNJsonConverter))]
		public decimal? GrossMargin { get; set; }
		[JsonConverter(typeof(NaNJsonConverter))]
		public decimal? OperatingMargin { get; set; }
		[JsonConverter(typeof(NaNJsonConverter))]
		public decimal? NetMargin { get; set; }
		[JsonConverter(typeof(NaNJsonConverter))]
		public decimal? Roe { get; set; }
		[JsonConverter(typeof(NaNJsonConverter))]
		public decimal? DebtToEquityRatio { get; set; }
		[JsonConverter(typeof(NaNJsonConverter))]
		public decimal? FinancialLeverage { get; set; }
		[JsonConverter(typeof(NaNJsonConverter))]
		public decimal? QuickRatio { get; set; }
		[JsonConverter(typeof(NaNJsonConverter))]
		public decimal? CurrentRatio { get; set; }
		[JsonConverter(typeof(NaNJsonConverter))]
		public decimal? DebtToEbitda { get; set; }
		[JsonConverter(typeof(NaNJsonConverter))]
		public decimal? AssetTurnover { get; set; }
		[JsonConverter(typeof(NaNJsonConverter))]
		public decimal? ReceivableTurnover { get; set; }
		[JsonConverter(typeof(NaNJsonConverter))]
		public decimal? ReturnOnAssetCurrent { get; set; }
		[JsonConverter(typeof(NaNJsonConverter))]
		public decimal? PriceToSalesRatio { get; set; }
		[JsonConverter(typeof(NaNJsonConverter))]
		public decimal? PriceToEarningsRatio { get; set; }
		[JsonConverter(typeof(NaNJsonConverter))]
		public decimal? PriceToBookRatio { get; set; }
		[JsonConverter(typeof(NaNJsonConverter))]
		public decimal? EvEbitda { get; set; }
	}
}
