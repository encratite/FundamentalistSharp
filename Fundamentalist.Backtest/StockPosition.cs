namespace Fundamentalist.Backtest
{
	internal class StockPosition
	{
		public string Symbol { get; set; }
		public decimal BuyPrice { get; set; }
		public long Count { get; set; }

		public StockPosition(string symbol, decimal buyPrice, long count)
		{
			Symbol = symbol;
			BuyPrice = buyPrice;
			Count = count;
		}
	}
}
