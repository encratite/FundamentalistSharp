using Fundamentalist.Common;
using Fundamentalist.Common.Json.FinancialStatement;

namespace Fundamentalist.Trainer
{
	internal class TickerCacheEntry
	{
		public List<FinancialStatement> FinancialStatements { get; set; }
		public List<PriceData> PriceData { get; set; }
	}
}
