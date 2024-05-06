using System.Collections.Concurrent;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;

namespace Fundamentalist.SqlAnalysis
{
	internal class SqlAnalysis
	{
		private const int MinimumDays = 20;

		public void Run(DateTime from, DateTime to, decimal upper, decimal lower, int limit, string form, string connectionString)
		{
			using (var connection = new SqlConnection(connectionString))
			{
				connection.Open();
				var factsTable = GetFactsTable(from, to, form, connection);
				var indexTable = GetIndexTable(from, to, limit, connection);
				var priceTable = GetPriceTable(from, to, limit, form, connection);
				var performance = CalculatePerformance(upper, lower, limit, indexTable, priceTable);
			}
		}

		private DataTable GetFactsTable(DateTime from, DateTime to, string form, SqlConnection connection)
		{
			Console.WriteLine("Loading facts");
			var stopwatch = new Stopwatch();
			stopwatch.Start();
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
			stopwatch.Stop();
			Console.WriteLine($"Finished loading facts in {stopwatch.Elapsed.TotalSeconds:F1} s");
			return dataTable;
		}

		private DataTable GetIndexTable(DateTime from, DateTime to, int limit, SqlConnection connection)
		{
			Console.WriteLine("Loading index price data");
			var stopwatch = new Stopwatch();
			stopwatch.Start();
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
			stopwatch.Stop();
			Console.WriteLine($"Finished loading index price data in {stopwatch.Elapsed.TotalSeconds:F1} s");
			return dataTable;
		}

		private DataTable GetPriceTable(DateTime from, DateTime to, int limit, string form, SqlConnection connection)
		{
			Console.WriteLine("Loading stock price data");
			var stopwatch = new Stopwatch();
			stopwatch.Start();
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
			stopwatch.Stop();
			Console.WriteLine($"Finished loading stock price data in {stopwatch.Elapsed.TotalSeconds:F1} s");
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
			var performanceValues = new ConcurrentDictionary<PriceKey, MeanAggregator>();
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
				performanceValues.AddOrUpdate(priceKey, new MeanAggregator(value), (priceKey, aggregator) =>
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
					GetDateOpenClose(firstRow, out date, out open, out close);
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
	}
}
