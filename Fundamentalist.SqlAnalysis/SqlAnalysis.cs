using System.Collections.Concurrent;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;

namespace Fundamentalist.SqlAnalysis
{
	internal class SqlAnalysis
	{
		private const int MinimumDays = 20;
		private const decimal MinimumFrequency = 0.01m;

		public void Run(DateTime from, DateTime to, decimal upper, decimal lower, int limit, string form, string connectionString)
		{
			using (var connection = new SqlConnection(connectionString))
			{
				Console.WriteLine("Loading SEC filings and price data from database");
				var stopwatch = new Stopwatch();
				stopwatch.Start();
				connection.Open();
				var factsTable = GetFactsTable(from, to, form, connection);
				var indexTable = GetIndexTable(from, to, limit, connection);
				var priceTable = GetPriceTable(from, to, limit, form, connection);
				stopwatch.Stop();
				Console.WriteLine($"Finished loading data in {stopwatch.Elapsed.TotalSeconds:F1} s");
				var performanceData = CalculatePerformance(upper, lower, limit, indexTable, priceTable);
				EvaluateFacts(factsTable, performanceData);
			}
		}

		private DataTable GetFactsTable(DateTime from, DateTime to, string form, SqlConnection connection)
		{
			string query = @"
				select
					symbol,
					filed,
					name
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
				parameters.AddWithValue("@from", from);
				parameters.AddWithValue("@to", to);
				parameters.AddWithValue("@form", form);
				using (var adapter = new SqlDataAdapter(command))
				{
					adapter.Fill(dataTable);
				}
			}
			return dataTable;
		}

		private DataTable GetIndexTable(DateTime from, DateTime to, int limit, SqlConnection connection)
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
				parameters.AddWithValue("@from", from);
				parameters.AddWithValue("@to", to);
				parameters.AddWithValue("@limit", limit);
				using (var adapter = new SqlDataAdapter(command))
				{
					adapter.Fill(dataTable);
				}
			}
			return dataTable;
		}

		private DataTable GetPriceTable(DateTime from, DateTime to, int limit, string form, SqlConnection connection)
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
				parameters.AddWithValue("@from", from);
				parameters.AddWithValue("@to", to);
				parameters.AddWithValue("@form", form);
				parameters.AddWithValue("@limit", limit);
				using (var adapter = new SqlDataAdapter(command))
				{
					adapter.Fill(dataTable);
				}
			}
			return dataTable;
		}

		private Dictionary<PriceKey, decimal> CalculatePerformance(decimal upper, decimal lower, int limit, DataTable indexTable, DataTable priceTable)
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
			Parallel.ForEach(factsTable.AsEnumerable(), row =>
			{
				string symbol = row.Field<string>("symbol");
				DateTime filed = row.Field<DateTime>("filed");
				string fact = row.Field<string>("name");
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
			var commonFacts = facts.Where(pair => (decimal)pair.Value.Count / performanceData.Count > MinimumFrequency).ToList();
			Parallel.ForEach(commonFacts, pair => pair.Value.UpdateStats());
			stopwatch.Stop();
			Console.WriteLine($"Finished evaluating facts in {stopwatch.Elapsed.TotalSeconds:F1} s");
			int rank = 1;
			foreach (var pair in commonFacts.OrderByDescending(pair => pair.Value.Mean))
			{
				string fact = pair.Key;
				var aggregator = pair.Value;
				var frequency = (decimal)aggregator.Count / performanceData.Count;
				if (frequency < MinimumFrequency)
					continue;
				Console.WriteLine($"{rank}. {fact}: μ = {aggregator.Mean:F3}, σ = {aggregator.StandardDeviation:F3} ({aggregator.Count}, {frequency:P2})");
				rank++;
			}
		}
	}
}
