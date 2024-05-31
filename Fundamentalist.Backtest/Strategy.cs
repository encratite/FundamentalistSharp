using System.Collections.ObjectModel;

namespace Fundamentalist.Backtest
{
	internal abstract class Strategy
	{
		private Backtest _backtest;

		public void SetBacktest(Backtest backtest)
		{
			_backtest = backtest;
		}

		public abstract void Next();

		protected DateTime Now => _backtest.Now;

		protected decimal Cash => _backtest.Cash;

		protected ReadOnlyCollection<StockPosition> Positions => _backtest.Positions;

		protected List<string> GetIndexComponents()
		{
			return _backtest.GetIndexComponents();
		}

		protected decimal? GetOpenPrice(string ticker, DateTime day)
		{
			return _backtest.GetOpenPrice(ticker, day);
		}

		protected decimal? GetClosePrice(string ticker, DateTime day)
		{
			return _backtest.GetClosePrice(ticker, day);
		}

		protected SortedList<DateTime, decimal> GetClosePrices(string ticker, DateTime from, DateTime to)
		{
			return _backtest.GetClosePrices(ticker, from, to);
		}

		protected Dictionary<string, SortedList<DateTime, decimal>> GetClosePrices(IEnumerable<string> tickers, DateTime from, DateTime to)
		{
			return _backtest.GetClosePrices(tickers, from, to);
		}

		protected long? GetBuyCount(string ticker, decimal targetSize)
		{
			return _backtest.GetBuyCount(ticker, targetSize);
		}

		protected void Buy(string ticker, long count)
		{
			_backtest.Buy(ticker, count);
		}

		protected void Sell(StockPosition position)
		{
			_backtest.Sell(position);
		}
	}
}
