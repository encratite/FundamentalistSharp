namespace Fundamentalist.Backtest
{
	public class StockPosition
	{
		public string Ticker { get; set; }
		public decimal AverageBuyPrice { get; set; }
		public long Count { get; set; }

		public StockPosition(string symbol, decimal buyPrice, long count)
		{
			Ticker = symbol;
			AverageBuyPrice = buyPrice;
			Count = count;
		}

		public override string ToString()
		{
			return $"{Ticker} ({Count})";
		}
	}
}
