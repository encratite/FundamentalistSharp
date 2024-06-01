namespace Fundamentalist.Backtest
{
	internal class StockPosition
	{
		public string Ticker { get; set; }
		public long Count { get; set; }

		public StockPosition(string symbol, long count)
		{
			Ticker = symbol;
			Count = count;
		}
	}
}
