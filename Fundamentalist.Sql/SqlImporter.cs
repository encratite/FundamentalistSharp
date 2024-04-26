using Fundamentalist.Common;
using Fundamentalist.Common.Json;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;

namespace Fundamentalist.Sql
{
	internal class SqlImporter
	{
		const string CompanyTable = "company";
		const string FactTable = "fact";
		const string PriceTable = "price";

		public void Import(string xbrlDirectory, string tickerPath, string priceDataDirectory, string connectionString)
		{
			var xbrlParser = new XbrlParser();
			xbrlParser.Load(xbrlDirectory, tickerPath, priceDataDirectory);
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			using (var connection = new SqlConnection(connectionString))
			{
				connection.Open();
				ImportCompanies(xbrlParser.Tickers, connection);
				ImportEarnings(xbrlParser.Earnings, connection);
				ImportPriceData(xbrlParser.Tickers, priceDataDirectory, connection);
				ImportIndexPriceData(priceDataDirectory, connection);
			}
			stopwatch.Stop();
			Console.WriteLine($"Imported all data into SQL database in {stopwatch.Elapsed.TotalSeconds:F1} s");
		}

		private void ImportCompanies(IEnumerable<Ticker> tickers, SqlConnection connection)
		{
			Console.WriteLine("Importing companies");
			TruncateTable(CompanyTable, connection);
			var table = new DataTable(CompanyTable);
			table.Columns.AddRange(new DataColumn[]
			{
				new DataColumn("cik", typeof(int)),
				new DataColumn("symbol", typeof(string)),
				new DataColumn("name", typeof(string))
			});
			foreach (var ticker in tickers)
				table.Rows.Add(ticker.Cik, ticker.Symbol, ticker.Title);
			using (var bulkCopy = GetBulkCopy(connection))
			{
				bulkCopy.DestinationTableName = CompanyTable;
				bulkCopy.WriteToServer(table);
			}
		}

		private void ImportEarnings(IEnumerable<CompanyEarnings> companyEarnings, SqlConnection connection)
		{
			Console.WriteLine("Importing earnings reports");
			TruncateTable(FactTable, connection);
			int i = 1;
			int count = companyEarnings.Count();
			foreach (var earnings in companyEarnings.OrderBy(x => x.Ticker.Symbol))
			{
				var ticker = earnings.Ticker;
				int cik = earnings.Ticker.Cik;
				PrintProgress(i, count, ticker);
				using (var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.TableLock | SqlBulkCopyOptions.UseInternalTransaction, null))
				{
					var table = new DataTable(FactTable);
					table.Columns.AddRange(new DataColumn[]
					{
						new DataColumn("cik", typeof(int)),
						new DataColumn("name", typeof(string)),
						new DataColumn("end_date", typeof(DateTime)),
						new DataColumn("value", typeof(decimal)),
						new DataColumn("fiscal_year", typeof(int)),
						new DataColumn("fiscal_period", typeof(string)),
						new DataColumn("form", typeof(string)),
						new DataColumn("filed", typeof(DateTime)),
						new DataColumn("frame", typeof(string)),
					});
					foreach (var fact in earnings.Facts.Values)
					{
						foreach (var pair in fact)
						{
							string factName = pair.Key;
							var factValues = pair.Value;
							table.Rows.Add(
								earnings.Ticker.Cik,
								factName,
								factValues.End,
								factValues.Value,
								factValues.FiscalYear,
								factValues.FiscalPeriod,
								factValues.Form,
								factValues.Filed,
								factValues.Frame
							);
						}
					}
					bulkCopy.DestinationTableName = FactTable;
					bulkCopy.WriteToServer(table);
				}
				i++;
			}
		}

		private void ImportPriceData(IEnumerable<Ticker> tickers, string priceDataDirectory, SqlConnection connection)
		{
			Console.WriteLine("Importing price data");
			TruncateTable(PriceTable, connection);
			int i = 1;
			int count = tickers.Count();
			foreach (var ticker in tickers.OrderBy(x => x.Symbol))
			{
				PrintProgress(i, count, ticker);
				var priceData = DataReader.GetPriceData(ticker.Symbol, priceDataDirectory);
				ImportTickerPriceData(ticker.Cik, priceData, connection);
				i++;
			}
		}

		private void ImportIndexPriceData(string priceDataDirectory, SqlConnection connection)
		{
			Console.WriteLine("Importing index price data");
			var priceData = DataReader.GetPriceData(DataReader.IndexTicker, priceDataDirectory);
			ImportTickerPriceData(null, priceData, connection);
		}

		private void ImportTickerPriceData(int? cik, SortedList<DateTime, PriceData> priceData, SqlConnection connection)
		{
			var table = new DataTable(PriceTable);
			table.Columns.AddRange(new DataColumn[]
			{
					new DataColumn("cik", typeof(int)),
					new DataColumn("date", typeof(DateTime)),
					new DataColumn("open_price", typeof(decimal)),
					new DataColumn("high", typeof(decimal)),
					new DataColumn("low", typeof(decimal)),
					new DataColumn("close_price", typeof(decimal)),
					new DataColumn("volume", typeof(long)),
			});
			using (var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.TableLock | SqlBulkCopyOptions.UseInternalTransaction, null))
			{
				foreach (var pair in priceData)
				{
					var date = pair.Key;
					var price = pair.Value;
					table.Rows.Add(
						cik,
						date,
						price.Open,
						price.High,
						price.Low,
						price.Close,
						price.Volume
					);
				}
				bulkCopy.DestinationTableName = PriceTable;
				bulkCopy.WriteToServer(table);
			}
		}

		private void PrintProgress(int i, int count, Ticker ticker)
		{
			Console.WriteLine($"Processing {ticker.Symbol} ({i}/{count})");
		}

		private void TruncateTable(string table, SqlConnection connection)
		{
			using (var command = new SqlCommand($"delete from {table}", connection))
				command.ExecuteNonQuery();
		}

		private SqlBulkCopy GetBulkCopy(SqlConnection connection)
		{
			var options = SqlBulkCopyOptions.TableLock | SqlBulkCopyOptions.UseInternalTransaction;
			return new SqlBulkCopy(connection, options, null);
		}
	}
}
