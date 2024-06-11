using Fundamentalist.Common;
using Fundamentalist.Common.Document;
using Fundamentalist.CsvImport.Document;
using MongoDB.Driver;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;

namespace Fundamentalist.Backtest
{
	internal class Backtest
	{
		private const bool EnableLogging = false;
		private const string DividendAction = "dividend";
		private const string DelistedAction = "delisted";

		private Configuration _configuration;
		private IMongoCollection<Price> _prices;
		private IMongoCollection<IndexComponents> _indexComponents;
		private IMongoCollection<TickerData> _tickers;
		private IMongoCollection<CorporateAction> _actions;
		private IMongoCollection<StrategyPerformance> _strategyPerformance;

		private DateTime? _now;
		private decimal? _cash;
		private Dictionary<string, StockPosition> _positions;

		private SortedList<DateTime, Price> _indexPrices;
		private Dictionary<string, List<Price>> _priceCache = new Dictionary<string, List<Price>>();
		private Dictionary<string, SortedList<DateTime, CorporateAction>> _actionCache;

		private StrategyPerformance _performance;

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

		public StrategyPerformance Run(Strategy strategy, Configuration configuration)
		{
			_configuration = configuration;
			InitializeCollections();
			_cash = _configuration.Cash;
			_positions = new Dictionary<string, StockPosition>();
			_performance = new StrategyPerformance
			{
				Name = strategy.Name,
				Description = strategy.Description
			};

			LoadIndexPriceData();
			LoadActions();
			_now = GetNextTradingDay(_configuration.From.Value);
			strategy.SetBacktest(this);
			strategy.Initialize();
			decimal accountValue = _cash.Value;
			var updateEquityCurve = () =>
			{
				accountValue = GetAccountValue();
				var equitySample = new EquitySample
				{
					Date = Now,
					Value = accountValue
				};
				_performance.EquityCurve.Add(equitySample);
			};
			while (_now.HasValue && _now < _configuration.To && _cash.Value > 0)
			{
				ProcessActions();
				updateEquityCurve();
				strategy.Next();
				var nextTradingDay = GetNextTradingDay(_now.Value.AddDays(1));
				if (nextTradingDay == null)
					break;
				_now = nextTradingDay;
			}
			updateEquityCurve();
			Log($"Final account value: {accountValue:C}");
			_performance.Time = DateTimeOffset.Now;
			return _performance;
		}

		public List<string> GetIndexComponents()
		{
			var filter = Builders<IndexComponents>.Filter.Lte(x => x.Date, _now.Value);
			var sort = Builders<IndexComponents>.Sort.Descending(x => x.Date);
			var indexComponents = _indexComponents.Find(filter).Sort(sort).FirstOrDefault();
			return indexComponents.Tickers;
		}

		public void PreCacheIndexComponents()
		{
			using var test = new PerformanceTimer("Caching index components", "Done caching index components");
			var tickers = new HashSet<string>();
			var indexComponents = _indexComponents.Find(Builders<IndexComponents>.Filter.Empty).ToList();
			foreach (var components in indexComponents)
			{
				foreach (string ticker in components.Tickers)
					tickers.Add(ticker);
			}
			var from = GetAdjustedFrom();
			var filter =
					Builders<Price>.Filter.In(x => x.Ticker, tickers) &
					Builders<Price>.Filter.Gte(x => x.Date, from);
			var prices = _prices.Find(filter).ToList();
			foreach (var price in prices)
			{
				List<Price> list;
				if (!_priceCache.TryGetValue(price.Ticker, out list))
				{
					list = new List<Price>();
					_priceCache[price.Ticker] = list;
				}
				list.Add(price);
			}
			foreach (var list in _priceCache.Values)
				list.Sort((x, y) => x.Date.CompareTo(y.Date));
		}

		public decimal? GetOpenPrice(string ticker, DateTime day)
		{
			var price = GetPrice(ticker, day);
			return price?.Open;
		}

		public decimal? GetUnadjustedOpenPrice(string ticker, DateTime day)
		{
			var price = GetPrice(ticker, day);
			return price?.UnadjustedOpen;
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
			var prices = GetCachedPrices(ticker);
			return prices.Where(x => x.Date >= from && x.Date < to).ToList();
		}

		public List<Price> GetPrices(string ticker, DateTime from, int count)
		{
			CheckDate(from);
			var prices = GetCachedPrices(ticker);
			var output = prices.Where(x => x.Date < from).TakeLast(count).ToList();
			return output;
		}

		public Dictionary<string, List<Price>> GetPrices(IEnumerable<string> tickers, DateTime from, DateTime to)
		{
			CheckFromTo(from, to);
			var output = new ConcurrentDictionary<string, List<Price>>();
			Parallel.ForEach(tickers, ticker =>
			{
				var prices = GetCachedPrices(ticker);
				output[ticker] = prices.Where(x => x.Date >= from && x.Date < to).ToList();
			});
			return output.ToDictionary();
		}

		public Dictionary<string, List<Price>> GetPrices(IEnumerable<string> tickers, DateTime from, int count)
		{
			CheckDate(from);
			var output = new ConcurrentDictionary<string, List<Price>>();
			Parallel.ForEach(tickers, ticker =>
			{
				var prices = GetCachedPrices(ticker);
				output[ticker] = prices.Where(x => x.Date < from).TakeLast(count).ToList();
			});
			return output.ToDictionary();
		}

		public long? GetBuyCount(string ticker, decimal targetSize)
		{
			var price = GetPrice(ticker, _now.Value);
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
			decimal fees = GetOrderFees(count, total);
			total += fees;
			if (total > _cash)
				return false;
			StockPosition position;
			if (!_positions.TryGetValue(ticker, out position))
			{
				position = new StockPosition(ticker, ask, count);
				_positions[ticker] = position;
			}
			else
			{
				long newCount = position.Count + count;
				position.AverageBuyPrice = (position.Count * position.AverageBuyPrice + total) / newCount;
				position.Count = newCount;
			}
			_cash -= total;
			var action = new StrategyAction
			{
				Time = Now,
				Action = StrategyActionEnum.Buy,
				Ticker = ticker,
				Price = ask,
				Count = count
			};
			_performance.Actions.Add(action);
			Log($"Bought {count} shares of {ticker} for {total:C}");
			return true;
		}

		public void Sell(string ticker, long count)
		{
			StockPosition position;
			if (!_positions.TryGetValue(ticker, out position))
				throw new ApplicationException("Unable to find ticker in positions");
			if (position.Count < count)
				throw new ApplicationException("Not enough shares available");
			var price = GetPrice(ticker, _now.Value);
			if (price == null)
				throw new ApplicationException("Unable to sell stock due to lack of price data");
			decimal bid = price.UnadjustedOpen;
			decimal total = count * bid;
			decimal fees = GetOrderFees(count, total);
			total -= fees;
			position.Count -= count;
			if (position.Count == 0)
				_positions.Remove(ticker);
			_cash += total;
			var action = new StrategyAction
			{
				Time = Now,
				Action = StrategyActionEnum.Sell,
				Ticker = ticker,
				Price = bid,
				Count = count
			};
			_performance.Actions.Add(action);
			decimal performance = bid / position.AverageBuyPrice - 1;
			Log($"Sold {count} shares of {ticker} for {total:C} ({performance:+#0.00%;-#0.00%;+0.00%})");
		}

		public TickerData GetTickerData(string ticker)
		{
			var filter = Builders<TickerData>.Filter.Eq(x => x.Ticker, ticker);
			var output = _tickers.Find(filter).FirstOrDefault();
			return output;
		}

		public decimal GetAccountValue()
		{
			decimal accountValue = Cash;
			foreach (var position in Positions.Values)
			{
				decimal? price = GetUnadjustedOpenPrice(position.Ticker, Now);
				if (!price.HasValue)
				{
					var lastPrice = GetLastPrice(position.Ticker, Now);
					price = lastPrice.UnadjustedClose;
				}
				accountValue += position.Count * price.Value;
			}
			return accountValue;
		}

		public void Log(string message)
		{
			if (EnableLogging)
				Console.WriteLine($"[{Now.ToShortDateString()}] {message}");
		}

		public void SavePerformance(StrategyPerformance performance)
		{
			_strategyPerformance.InsertOne(performance);
		}

		private void InitializeCollections()
		{
			var database = Utility.GetMongoDatabase(_configuration.ConnectionString);
			var collections = database.ListCollectionNames().ToList();
			if (!collections.Contains(Collection.StrategyPerformance))
				database.CreateCollection(Collection.StrategyPerformance);

			_prices = database.GetCollection<Price>(Collection.Prices);
			_indexComponents = database.GetCollection<IndexComponents>(Collection.IndexComponents);
			_tickers = database.GetCollection<TickerData>(Collection.Tickers);
			_actions = database.GetCollection<CorporateAction>(Collection.Actions);
			_strategyPerformance = database.GetCollection<StrategyPerformance>(Collection.StrategyPerformance);
		}

		private decimal GetOrderFees(long count, decimal total)
		{
			decimal fees = count * _configuration.FeesPerShare.Value;
			fees = Math.Max(_configuration.MinimumFeesPerOrder.Value, fees);
			fees = Math.Min(_configuration.MaximumFeesPerOrderRatio.Value * total, fees);
			return fees;
		}

		private decimal GetAsk(Price price)
		{
			decimal ask = price.UnadjustedOpen * (1 + _configuration.Spread.Value);
			return ask;
		}

		private void LoadIndexPriceData()
		{
			if (_indexPrices != null)
				return;
			_indexPrices = new SortedList<DateTime, Price>();
			var filter = Builders<Price>.Filter.Eq(x => x.Ticker, null);
			var indexPrices = _prices.Find(filter).ToList();
			foreach (var price in indexPrices)
				_indexPrices[price.Date] = price;
		}

		private void LoadActions()
		{
			if (_actionCache != null)
				return;
			using var test = new PerformanceTimer("Caching corporate actions", "Done caching corporate actions");
			_actionCache = new Dictionary<string, SortedList<DateTime, CorporateAction>>();
			var filter = Builders<CorporateAction>.Filter.Eq(x => x.Action, DividendAction) | Builders<CorporateAction>.Filter.Eq(x => x.Action, DelistedAction);
			var actions = _actions.Find(filter).ToList();
			foreach (var action in actions)
			{
				string key = action.Ticker;
				SortedList<DateTime, CorporateAction> tickerActions;
				if (!_actionCache.TryGetValue(key, out tickerActions))
				{
					tickerActions = new SortedList<DateTime, CorporateAction>();
					_actionCache[key] = tickerActions;
				}
				tickerActions[action.Date] = action;
			}
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
			var prices = GetCachedPrices(ticker);
			return prices.FirstOrDefault(x => x.Date == day);
		}

		private Price GetLastPrice(string ticker, DateTime day)
		{
			CheckDate(day);
			var prices = GetCachedPrices(ticker);
			return prices.Where(x => x.Date < day).LastOrDefault();
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

		private List<Price> GetCachedPrices(string ticker)
		{
			if (ticker == null)
				return _indexPrices.Values.ToList();
			List<Price> output;
			if (_priceCache.TryGetValue(ticker, out output))
				return output;
			var from = GetAdjustedFrom();
			var filter =
				Builders<Price>.Filter.Eq(x => x.Ticker, ticker) &
				Builders<Price>.Filter.Gte(x => x.Date, from);
			output = _prices.Find(filter).ToList().OrderBy(x => x.Date).ToList();
			_priceCache[ticker] = output;
			return output;
		}

		private DateTime GetAdjustedFrom()
		{
			DateTime from = _configuration.From.Value.AddYears(-1);
			return from;
		}

		private void ProcessActions()
		{
			foreach (var position in _positions.Values)
			{
				SortedList<DateTime, CorporateAction> tickerActions;
				if (!_actionCache.TryGetValue(position.Ticker, out tickerActions))
					continue;
				CorporateAction action;
				if (!tickerActions.TryGetValue(Now, out action))
					continue;
				if (action.Action == DividendAction)
				{
					decimal dividends = position.Count * action.Value.Value;
					_cash += dividends;
					Log($"Received {dividends:C} worth of dividends for {position.Ticker}");
				}
				else if (action.Action == DelistedAction)
				{
					Log($"{position.Ticker} has been delisted");
					var price = GetLastPrice(position.Ticker, Now);
					Sell(position.Ticker, position.Count);
				}
				else
					throw new ApplicationException("Encountered an unknown corporate action");
			}
		}
	}
}
