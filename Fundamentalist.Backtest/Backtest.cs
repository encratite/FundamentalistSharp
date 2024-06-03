using Fundamentalist.Common;
using Fundamentalist.Common.Document;
using Fundamentalist.CsvImport.Document;
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

		public ReadOnlyDictionary<string, StockPosition> Positions
		{
			get => _positions.AsReadOnly();
		}

		private Configuration _configuration;
		private IMongoCollection<Price> _prices;
		private IMongoCollection<IndexComponents> _indexComponents;
		private IMongoCollection<TickerData> _tickers;

		private DateTime? _now;
		private decimal? _cash;
		private Dictionary<string, StockPosition> _positions;

		private SortedList<DateTime, Price> _indexPrices;

		public void Run(Strategy strategy, Configuration configuration)
		{
			_configuration = configuration;
			var database = Utility.GetMongoDatabase(_configuration.ConnectionString);
			_prices = database.GetCollection<Price>(Collection.Prices);
			_indexComponents = database.GetCollection<IndexComponents>(Collection.IndexComponents);
			_tickers = database.GetCollection<TickerData>(Collection.Tickers);
			_cash = _configuration.Cash;
			_positions = new Dictionary<string, StockPosition>();

			LoadIndexPriceData();
			_now = GetNextTradingDay(_configuration.From.Value);
			strategy.SetBacktest(this);
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
			return price.Open;
		}

		public decimal? GetClosePrice(string ticker, DateTime day)
		{
			if (day == _now)
				throw new ApplicationException("Retrieving the close price of the current day is not permitted");
			var price = GetPrice(ticker, day);
			return price?.Close;
		}

		public List<Price> GetPrices(string ticker, DateTime from, DateTime to)
		{
			CheckFromTo(from, to);
			var filter =
				Builders<Price>.Filter.Eq(x => x.Ticker, ticker) &
				Builders<Price>.Filter.Gte(x => x.Date, from) &
				Builders<Price>.Filter.Lt(x => x.Date, to);
			var output = _prices.Find(filter).ToList();
			return output;
		}

		public List<Price> GetPrices(string ticker, DateTime from, int count)
		{
			CheckDate(from);
			var filter =
				Builders<Price>.Filter.Eq(x => x.Ticker, ticker) &
				Builders<Price>.Filter.Lt(x => x.Date, from);
			var output = GetPricesWithLimit(filter, count);
			return output;
		}

		public Dictionary<string, List<Price>> GetPrices(IEnumerable<string> tickers, DateTime from, DateTime to)
		{
			CheckFromTo(from, to);
			var filter =
				Builders<Price>.Filter.In(x => x.Ticker, tickers) &
				Builders<Price>.Filter.Gte(x => x.Date, from) &
				Builders<Price>.Filter.Lt(x => x.Date, to);
			var prices = _prices.Find(filter).ToList();
			var output = GetPricesByTicker(prices);
			return output;
		}

		public Dictionary<string, List<Price>> GetPrices(IEnumerable<string> tickers, DateTime from, int count)
		{
			CheckDate(from);
			var filter =
				Builders<Price>.Filter.In(x => x.Ticker, tickers) &
				Builders<Price>.Filter.Lt(x => x.Date, from);
			int adjustedCount = tickers.Count() * count;
			var prices = GetPricesWithLimit(filter, adjustedCount);
			var output = GetPricesByTicker(prices, count);
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

		public bool Buy(string ticker, long count)
		{
			var price = GetPrice(ticker, _now.Value);
			if (price == null)
				throw new ApplicationException("Unable to buy stock due to lack of price data");
			decimal ask = GetAsk(price);
			decimal total = count * ask;
			if (total > _cash)
				return false;
			StockPosition position;
			if (!_positions.TryGetValue(ticker, out position))
			{
				position = new StockPosition(ticker, count);
				_positions[ticker] = position;
			}
			else
				position.Count += count;
			_cash -= total;
			return true;
		}

		public void Sell(string ticker, long count)
		{
			StockPosition position;
			if (!_positions.TryGetValue(ticker, out position))
				throw new ApplicationException("Unable to find ticker in positions");
			if (position.Count < count)
				throw new ApplicationException("Not enough shares available");
			// This is a hack to deal with acquisitions
			var price = GetLastPrice(ticker, _now.Value);
			if (price == null)
				throw new ApplicationException("Unable to sell stock due to lack of price data");
			decimal bid = price.Open;
			decimal total = position.Count * bid;
			position.Count -= count;
			if (position.Count == 0)
				_positions.Remove(ticker);
			_cash += total;
		}

		public TickerData GetTickerData(string ticker)
		{
			var filter = Builders<TickerData>.Filter.Eq(x => x.Ticker, ticker);
			var output = _tickers.Find(filter).FirstOrDefault();
			return output;
		}

		private decimal GetAsk(Price price)
		{
			decimal ask = price.Open * (1 + _configuration.Spread.Value);
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
			CheckDate(day);
			var filter = Builders<Price>.Filter.Eq(x => x.Ticker, ticker) & Builders<Price>.Filter.Eq(x => x.Date, day);
			var price = _prices.Find(filter).FirstOrDefault();
			return price;
		}

		private Price GetLastPrice(string ticker, DateTime day)
		{
			CheckDate(day);
			var filter = Builders<Price>.Filter.Eq(x => x.Ticker, ticker) & Builders<Price>.Filter.Lte(x => x.Date, day);
			var sort = Builders<Price>.Sort.Descending(x => x.Date);
			var price = _prices.Find(filter).Sort(sort).Limit(1).FirstOrDefault();
			return price;
		}

		private void CheckDate(DateTime day)
		{
			if (day > _now)
				throw new ApplicationException("Reading price data from the future is not permitted");
		}

		private void CheckFromTo(DateTime from, DateTime to)
		{
			if (from > to)
				throw new ApplicationException("Invalid timestamps specified");
		}

		private Dictionary<string, List<Price>> GetPricesByTicker(List<Price> prices, int? limit = null)
		{
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
				if (!limit.HasValue || tickerPrices.Count < limit.Value)
					tickerPrices.Add(price);
			}
			return output;
		}

		private List<Price> GetPricesWithLimit(FilterDefinition<Price> filter, int count)
		{
			var sort = Builders<Price>.Sort.Descending(x => x.Date);
			var output = _prices
				.Find(filter)
				.Sort(sort)
				.Limit(count)
				.ToList()
				.OrderBy(x => x.Date)
				.ToList();
			return output;
		}
	}
}
