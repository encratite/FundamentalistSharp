using Fundamentalist.Common.Document;
using Fundamentalist.CsvImport.Document;
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

		public virtual void Initialize()
		{
		}

		public abstract void Next();

		protected DateTime Now => _backtest.Now;

		protected decimal Cash => _backtest.Cash;

		protected ReadOnlyDictionary<string, StockPosition> Positions => _backtest.Positions;

		protected List<string> GetIndexComponents()
		{
			return _backtest.GetIndexComponents();
		}

		protected void PreCacheIndexComponents()
		{
			_backtest.PreCacheIndexComponents();
		}

		protected decimal? GetOpenPrice(string ticker, DateTime day)
		{
			return _backtest.GetOpenPrice(ticker, day);
		}

		protected decimal? GetClosePrice(string ticker, DateTime day)
		{
			return _backtest.GetClosePrice(ticker, day);
		}

		protected List<Price> GetPrices(string ticker, DateTime from, DateTime to)
		{
			return _backtest.GetPrices(ticker, from, to);
		}

		protected List<Price> GetPrices(string ticker, DateTime from, int count)
		{
			return _backtest.GetPrices(ticker, from, count);
		}

		protected Dictionary<string, List<Price>> GetPrices(IEnumerable<string> tickers, DateTime from, DateTime to)
		{
			return _backtest.GetPrices(tickers, from, to);
		}

		protected Dictionary<string, List<Price>> GetPrices(IEnumerable<string> tickers, DateTime from, int count)
		{
			return _backtest.GetPrices(tickers, from, count);
		}

		protected long? GetBuyCount(string ticker, decimal targetSize)
		{
			return _backtest.GetBuyCount(ticker, targetSize);
		}

		protected bool Buy(string ticker, long count)
		{
			return _backtest.Buy(ticker, count);
		}

		protected void Sell(string ticker, long count)
		{
			_backtest.Sell(ticker, count);
		}

		protected TickerData GetTickerData(string ticker)
		{
			return _backtest.GetTickerData(ticker);
		}

		protected void Log(string message)
		{
			_backtest.Log(message);
		}
	}
}
