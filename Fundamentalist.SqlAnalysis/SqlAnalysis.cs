using System.Collections.Concurrent;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Text.Json;

namespace Fundamentalist.SqlAnalysis
{
	internal class SqlAnalysis
	{
		private const int MinimumDays = 20;

		private Configuration _configuration;

		public void Run(Configuration configuration)
		{
			_configuration = configuration;
			Console.WriteLine("Loading SEC filings and price data from database");
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			DataTable factsTable, indexTable, priceTable;
			using (var connection = new SqlConnection(_configuration.ConnectionString))
			{
				connection.Open();
				factsTable = GetFactsTable(connection);
				indexTable = GetIndexTable(connection);
				priceTable = GetPriceTable(connection);
			}
			stopwatch.Stop();
			Console.WriteLine($"Finished loading data in {stopwatch.Elapsed.TotalSeconds:F1} s");
			var performanceData = CalculatePerformance(indexTable, priceTable);
			EvaluateFacts(factsTable, performanceData);
		}

		private DataTable GetFactsTable(SqlConnection connection)
		{
			string query = @"
				select
					symbol,
					filed,
					name,
					unit
				from
					fact join ticker
					on fact.cik = ticker.cik
				where
					fact.filed >= @from
					and fact.filed < @to
					and fact.form = @form
					and ticker.exclude = 0";
			var dataTable = new DataTable();
			using (var command = new SqlCommand(query, connection))
			{
				var parameters = command.Parameters;
				parameters.AddWithValue("@from", _configuration.From.Value);
				parameters.AddWithValue("@to", _configuration.To.Value);
				parameters.AddWithValue("@form", _configuration.Form);
				using (var adapter = new SqlDataAdapter(command))
				{
					adapter.Fill(dataTable);
				}
			}
			return dataTable;
		}

		private DataTable GetIndexTable(SqlConnection connection)
		{
			string query = @"
				select
					date,
					open_price,
					close_price
				from price
				where
					symbol is null
					and date >= @from
					and date <= dateadd(day, @limit, @from)
				order by date";
			var dataTable = new DataTable();
			using (var command = new SqlCommand(query, connection))
			{
				var parameters = command.Parameters;
				parameters.AddWithValue("@from", _configuration.From.Value);
				parameters.AddWithValue("@to", _configuration.To.Value);
				parameters.AddWithValue("@limit", _configuration.Limit.Value);
				using (var adapter = new SqlDataAdapter(command))
				{
					adapter.Fill(dataTable);
				}
			}
			return dataTable;
		}

		private DataTable GetPriceTable(SqlConnection connection)
		{
			string query = @"
				select
					S.symbol,
					filed,
					date,
					open_price,
					close_price
				from
				(
					select distinct
						symbol,
						filed
					from
						fact join ticker
						on fact.cik = ticker.cik
					where
						fact.filed >= @from
						and fact.filed < @to
						and fact.form = @form
						and ticker.exclude = 0
				) as S join price
				on S.symbol = price.symbol
				where
					price.date >= @from
					and price.date <= dateadd(day, @limit, @from)
				order by S.symbol, filed, date";
			var dataTable = new DataTable();
			using (var command = new SqlCommand(query, connection))
			{
				var parameters = command.Parameters;
				parameters.AddWithValue("@from", _configuration.From.Value);
				parameters.AddWithValue("@to", _configuration.To.Value);
				parameters.AddWithValue("@form", _configuration.Form);
				parameters.AddWithValue("@limit", _configuration.Limit.Value);
				using (var adapter = new SqlDataAdapter(command))
				{
					adapter.Fill(dataTable);
				}
			}
			return dataTable;
		}

		private Dictionary<PriceKey, decimal> CalculatePerformance(DataTable indexTable, DataTable priceTable)
		{
			Console.WriteLine("Calculating performance values");
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			var indexPrices = new Dictionary<DateTime, OpenClosePrice>();
			foreach (DataRow row in indexTable.Rows)
			{
				DateTime date = row.Field<DateTime>("date");
				decimal open = row.Field<decimal>("open_price");
				decimal close = row.Field<decimal>("close_price");
				var openClosePrice = new OpenClosePrice(open, close);
				indexPrices[date] = openClosePrice;
			}
			var performanceValues = new ConcurrentDictionary<PriceKey, StatsAggregator>();
			PriceKey priceKey = null;
			int offset = 0;
			var symbolRows = new List<RowRange>();
			for (int i = 0; i < priceTable.Rows.Count; i++)
			{
				var row = priceTable.Rows[i];
				string symbol = row.Field<string>("symbol");
				DateTime filed = row.Field<DateTime>("filed");
				var currentPriceKey = new PriceKey(symbol, filed);
				if (!currentPriceKey.Equals(priceKey))
				{
					if (priceKey != null && i - offset >= MinimumDays)
					{
						var rowRange = new RowRange(priceKey, offset, i);
						symbolRows.Add(rowRange);
					}
					priceKey = currentPriceKey;
					offset = i;
				}
			}
			Action<decimal, PriceKey> aggregatePerformance = (value, priceKey) =>
			{
				performanceValues.AddOrUpdate(priceKey, new StatsAggregator(value), (priceKey, aggregator) =>
				{
					aggregator.Add(value);
					return aggregator;
				});
			};
			decimal upper = _configuration.Upper.Value;
			decimal lower = _configuration.Lower.Value;
			Func<decimal, PriceKey, bool> boundaryCheck = (performance, priceKey) =>
			{
				decimal? value = null;
				if (performance > upper)
					value = upper;
				else if (performance < lower)
					value = lower;
				if (value.HasValue)
				{
					aggregatePerformance(value.Value, priceKey);
					return true;
				}
				else
					return false;
			};
			Parallel.ForEach(symbolRows, rowRange =>
			{
				var key = rowRange.Key;
				DateTime date;
				decimal open, close, firstClose;
				close = 0m;
				var firstRow = priceTable.Rows[rowRange.Start];
				GetDateOpenClose(firstRow, out date, out open, out firstClose);
				var firstIndexPrice = indexPrices[date];
				OpenClosePrice currentIndexPrice = null;
				for (int i = rowRange.Start + 1; i < rowRange.End; i++)
				{
					var row = priceTable.Rows[i];
					GetDateOpenClose(row, out date, out open, out close);
					currentIndexPrice = indexPrices[date];
					var openPerformance = GetPerformance(firstClose, open, firstIndexPrice.Close, currentIndexPrice.Open);
					if (boundaryCheck(openPerformance, key))
						return;
					var closePerformance = GetPerformance(firstClose, close, firstIndexPrice.Close, currentIndexPrice.Close);
					if (boundaryCheck(closePerformance, key))
						return;
				}
				var finalPerformance = GetPerformance(firstClose, close, firstIndexPrice.Close, currentIndexPrice.Close);
				aggregatePerformance(finalPerformance, key);
			});
			var output = new Dictionary<PriceKey, decimal>();
			foreach (var pair in performanceValues)
				output[pair.Key] = pair.Value.Mean;
			stopwatch.Stop();
			Console.WriteLine($"Finished calculating performance values in {stopwatch.Elapsed.TotalSeconds:F1} s");
			return output;
		}

		private void GetDateOpenClose(DataRow row, out DateTime date, out decimal open, out decimal close)
		{
			date = row.Field<DateTime>("date");
			open = row.Field<decimal>("open_price");
			close = row.Field<decimal>("close_price");
		}

		private decimal GetPerformance(decimal stockFrom, decimal stockTo, decimal indexFrom, decimal indexTo)
		{
			return stockTo / stockFrom - indexTo / indexFrom;
		}

		private void EvaluateFacts(DataTable factsTable, Dictionary<PriceKey, decimal> performanceData)
		{
			Console.WriteLine("Evaluating facts");
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			var facts = new ConcurrentDictionary<string, StatsAggregator>();
			var units = new ConcurrentDictionary<string, string>();
			Parallel.ForEach(factsTable.AsEnumerable(), row =>
			{
				string symbol = row.Field<string>("symbol");
				DateTime filed = row.Field<DateTime>("filed");
				string fact = row.Field<string>("name");
				string unit = row.Field<string>("unit");
				if (!units.ContainsKey(fact))
					units[fact] = unit;
				var priceKey = new PriceKey(symbol, filed);
				decimal performance;
				if (!performanceData.TryGetValue(priceKey, out performance))
					return;
				facts.AddOrUpdate(fact, new StatsAggregator(performance), (fact, aggregator) =>
				{
					aggregator.Add(performance);
					return aggregator;
				});
			});
			var commonFacts = facts.Where(pair => (decimal)pair.Value.Count / performanceData.Count > _configuration.MinimumFrequency.Value).ToList();
			Parallel.ForEach(commonFacts, pair => pair.Value.UpdateStats());
			stopwatch.Stop();
			Console.WriteLine($"Finished evaluating facts in {stopwatch.Elapsed.TotalSeconds:F1} s");
			using (var writer = new StreamWriter(_configuration.Output))
			{
				string json = JsonSerializer.Serialize(_configuration);
				writer.WriteLine("Configuration used:");
				writer.WriteLine(json);
				writer.WriteLine(string.Empty);
				var writeStats = (IOrderedEnumerable<KeyValuePair<string, StatsAggregator>> orderedFacts) =>
				{
					int rank = 1;
					foreach (var pair in orderedFacts)
					{
						string fact = pair.Key;
						var aggregator = pair.Value;
						var frequency = (decimal)aggregator.Count / performanceData.Count;
						if (frequency < _configuration.MinimumFrequency.Value)
							continue;
						string unit = units[fact];
						writer.WriteLine($"{rank}. {fact} ({unit}): μ = {aggregator.Mean:F3}, σ = {aggregator.StandardDeviation:F3} ({aggregator.Count}, {frequency:P2})");
						rank++;
					}
				};
				writer.WriteLine("Results ordered by μ:");
				writeStats(commonFacts.OrderByDescending(pair => pair.Value.Mean));
				writer.WriteLine(string.Empty);
				writer.WriteLine("Results ordered by σ:");
				writeStats(commonFacts.OrderBy(pair => pair.Value.StandardDeviation));
			}
			Console.WriteLine($"Wrote results to \"{_configuration.Output}\"");
		}
	}
}
