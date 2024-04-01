using Fundamentalist.Common;
using Fundamentalist.Common.Json.FinancialStatement;
using Fundamentalist.Common.Json.KeyRatios;

namespace Fundamentalist.Trainer
{
	internal class TickerCacheEntry
	{
		public List<FinancialStatement> FinancialStatements { get; set; }
		public KeyRatios KeyRatios { get; set; }
		public List<PriceData> PriceData { get; set; }
	}
}
