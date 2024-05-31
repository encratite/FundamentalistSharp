using Fundamentalist.Common;
using Fundamentalist.Common.Document;
using MongoDB.Driver;
using System.Collections.ObjectModel;

namespace Fundamentalist.Backtest
{
	internal class Backtest
	{
		public DateTime Now
		{
			get => _now.Value;
		}

		public decimal Cash
		{
			get => _cash.Value;
		}

		public ReadOnlyCollection<StockPosition> Positions
		{
			get => _positions.AsReadOnly();
		}

		private Configuration _configuration;
		private IMongoCollection<Price> _prices;
		private IMongoCollection<IndexComponents> _indexComponents;

		private DateTime? _now;
		private decimal? _cash;
		private List<StockPosition> _positions;

		private SortedList<DateTime, Price> _indexPrices;

		public void Run(Strategy strategy, Configuration configuration)
		{
			_configuration = configuration;
			var database = Utility.GetMongoDatabase(_configuration.ConnectionString);
			_prices = database.GetCollection<Price>(Collection.Prices);
			_indexComponents = database.GetCollection<IndexComponents>(Collection.IndexComponents);
			_cash = _configuration.Cash;
			_positions = new List<StockPosition>();

			LoadIndexPriceData();
			_now = GetNextTradingDay(_configuration.From.Value);

			while (_now.HasValue && _now < _configuration.To && _cash.Value > 0)
			{
				strategy.Next();
				_now = GetNextTradingDay(_now.Value.AddDays(1));
			}
		}

		public List<string> GetIndexComponents()
		{
			var filter = Builders<IndexComponents>.Filter.Lte(x => x.Date, _now.Value);
			var sort = Builders<IndexComponents>.Sort.Descending(x => x.Date);
			var indexComponents = _indexComponents.Find(filter).Sort(sort).FirstOrDefault();
			return indexComponents.Tickers;
		}

		public decimal? GetOpenPrice(string ticker, DateTime day)
		{
			var price = GetPrice(ticker, day);
			if (price == null)
				return null;
			decimal open = price.GetUnadjustedOpen();
			return open;
		}

		public decimal? GetClosePrice(string ticker, DateTime day)
		{
			if (day == _now)
				throw new ApplicationException("Retrieving the close price of the current day is not permitted");
			var price = GetPrice(ticker, day);
			return price?.Close;
		}

		public SortedList<DateTime, decimal> GetClosePrices(string ticker, DateTime from, DateTime to)
		{
			var prices = GetPrices(ticker, from, to);
			var output = new SortedList<DateTime, decimal>();
			foreach (var price in prices)
				output[price.Date] = price.UnadjustedClose.Value;
			return output;
		}

		public Dictionary<string, SortedList<DateTime, decimal>> GetClosePrices(IEnumerable<string> tickers, DateTime from, DateTime to)
		{
			var prices = GetPrices(tickers, from, to);
			var output = new Dictionary<string, SortedList<DateTime, decimal>>();
			foreach (var pair in prices)
			{
				var closePrices = new SortedList<DateTime, decimal>();
				foreach (var price in pair.Value)
					closePrices[price.Date] = price.UnadjustedClose.Value;
				output[pair.Key] = closePrices;
			}
			return output;
		}

		public long? GetBuyCount(string ticker, decimal targetSize)
		{
			var price = GetPrice(ticker, _now.Value);
			if (price == null)
				return null;
			decimal ask = GetAsk(price);
			long count = (long)Math.Floor(targetSize / ask);
			return count;
		}

		public void Buy(string ticker, long count)
		{
			var price = GetPrice(ticker, _now.Value);
			if (price == null)
				throw new ApplicationException("Unable to buy stock due to lack of price data");
			decimal ask = GetAsk(price);
			decimal total = count * ask;
			if (total > _cash)
				throw new ApplicationException("Not enough money to buy this stock");
			decimal open = price.GetUnadjustedOpen();
			var position = new StockPosition(ticker, open, count);
			_positions.Add(position);
			_cash -= total;
		}

		public void Sell(StockPosition position)
		{
			var price = GetPrice(position.Ticker, _now.Value);
			if (price == null)
				throw new ApplicationException("Unable to sell stock due to lack of price data");
			decimal bid = price.GetUnadjustedOpen();
			decimal total = position.Count * bid;
			_positions.Remove(position);
			_cash += total;
		}

		private decimal GetAsk(Price price)
		{
			decimal open = price.GetUnadjustedOpen();
			decimal ask = open * (1 + _configuration.Spread.Value);
			return ask;
		}

		private void LoadIndexPriceData()
		{
			_indexPrices = new SortedList<DateTime, Price>();
			var filter = Builders<Price>.Filter.Eq(x => x.Ticker, null);
			var indexPrices = _prices.Find(filter).ToList();
			foreach (var price in indexPrices)
				_indexPrices[price.Date] = price;
		}

		private DateTime? GetNextTradingDay(DateTime start)
		{
			for (DateTime day = start; day < _configuration.To; day = day.AddDays(1))
			{
				if (_indexPrices.ContainsKey(day))
					return day;
			}
			return null;
		}

		private Price GetPrice(string ticker, DateTime day)
		{
			CheckDate(day, false);
			var filter = Builders<Price>.Filter.Eq(x => x.Ticker, ticker) & Builders<Price>.Filter.Eq(x => x.Date, day);
			var price = _prices.Find(filter).FirstOrDefault();
			return price;
		}

		private List<Price> GetPrices(string ticker, DateTime from, DateTime to)
		{
			CheckFromTo(from, to);
			CheckDate(to, true);
			var filter =
				Builders<Price>.Filter.Eq(x => x.Ticker, ticker) &
				Builders<Price>.Filter.Gte(x => x.Date, from) &
				Builders<Price>.Filter.Lt(x => x.Date, to);
			var output = _prices.Find(filter).ToList();
			return output;
		}

		private Dictionary<string, List<Price>> GetPrices(IEnumerable<string> tickers, DateTime from, DateTime to)
		{
			CheckFromTo(from, to);
			CheckDate(to, true);
			var filter =
				Builders<Price>.Filter.In(x => x.Ticker, tickers) &
				Builders<Price>.Filter.Gte(x => x.Date, from) &
				Builders<Price>.Filter.Lt(x => x.Date, to);
			var prices = _prices.Find(filter).ToList();
			var output = new Dictionary<string, List<Price>>();
			foreach (var price in prices)
			{
				List<Price> tickerPrices;
				string key = price.Ticker;
				if (!output.TryGetValue(key, out tickerPrices))
				{
					tickerPrices = new List<Price>();
					output[key] = tickerPrices;
				}
				tickerPrices.Add(price);
			}
			return output;
		}

		private void CheckDate(DateTime day, bool greaterOrEqual)
		{
			if (day > _now || (greaterOrEqual && day == _now))
				throw new ApplicationException("Reading price data from the future is not permitted");
		}

		private void CheckFromTo(DateTime from, DateTime to)
		{
			if (from > to)
				throw new ApplicationException("Invalid timestamps specified");
		}
	}
}
