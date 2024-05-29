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
		private IMongoDatabase _database;

		private DateTime? _now;
		private decimal? _cash;
		private List<StockPosition> _positions;

		private SortedList<DateTime, Price> _indexPrices;

		public void Run(Strategy strategy, Configuration configuration)
		{
			_configuration = configuration;
			_database = Utility.GetMongoDatabase(_configuration.ConnectionString);
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
			var collection = _database.GetCollection<IndexComponents>(Collection.IndexComponents);
			var filter = Builders<IndexComponents>.Filter.Lte(x => x.Date, _now.Value);
			var sort = Builders<IndexComponents>.Sort.Descending(x => x.Date);
			var indexComponents = collection.Find(filter).Sort(sort).FirstOrDefault();
			return indexComponents.Tickers;
		}

		public decimal? GetOpenPrice(string symbol, DateTime day)
		{
			var price = GetPrice(symbol, day);
			if (price == null)
				return null;
			decimal open = price.GetUnadjustedOpen();
			return open;
		}

		public decimal? GetClosePrice(string symbol, DateTime day)
		{
			if (day == _now)
				throw new ApplicationException("Retrieving the close price of the current day is not permitted");
			var price = GetPrice(symbol, day);
			return price?.Close;
		}

		public void Buy(string symbol, long count)
		{
			var price = GetPrice(symbol, _now.Value, true);
			decimal open = price.GetUnadjustedOpen();
			decimal ask = open * (1 + _configuration.Spread.Value);
			decimal total = count * ask;
			if (total > _cash)
				throw new ApplicationException("Not enough money to buy this stock");
			var position = new StockPosition(symbol, open, count);
			_positions.Add(position);
			_cash -= total;
		}

		public void Sell(StockPosition position)
		{
			var price = GetPrice(position.Symbol, _now.Value, true);
			decimal bid = price.GetUnadjustedOpen();
			decimal total = position.Count * bid;
			_positions.Remove(position);
			_cash += total;
		}

		private void LoadIndexPriceData()
		{
			_indexPrices = new SortedList<DateTime, Price>();
			var prices = _database.GetCollection<Price>(Collection.Prices);
			var filter = Builders<Price>.Filter.Eq(x => x.Ticker, null);
			var indexPrices = prices.Find(filter).ToList();
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

		private Price GetPrice(string symbol, DateTime day, bool throwIfNotAvailable = false)
		{
			if (day > _now)
				throw new ApplicationException("Reading price data from the future is not permitted");
			var prices = _database.GetCollection<Price>(Collection.Prices);
			var filter = Builders<Price>.Filter.Eq(x => x.Ticker, symbol) & Builders<Price>.Filter.Eq(x => x.Date, day);
			var price = prices.Find(filter).FirstOrDefault();
			if (price == null && throwIfNotAvailable)
				throw new ApplicationException("Stock not available");
			return price;
		}
	}
}
