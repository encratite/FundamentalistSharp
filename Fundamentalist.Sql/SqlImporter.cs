using Fundamentalist.Common;
using Fundamentalist.Common.Json;
using Npgsql;
using NpgsqlTypes;
using System.Diagnostics;

namespace Fundamentalist.Sql
{
	internal class SqlImporter
	{
		public void Import(string xbrlDirectory, string tickerPath, string priceDataDirectory, string connectionString)
		{
			var xbrlParser = new XbrlParser();
			xbrlParser.Load(xbrlDirectory, tickerPath, priceDataDirectory);
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			using (var connection = new NpgsqlConnection(connectionString))
			{
				connection.Open();
				ImportCompanies(xbrlParser.Tickers, connection);
				ImportEarnings(xbrlParser.Earnings, connection);
				ImportPriceData(xbrlParser.Tickers, priceDataDirectory, connection);
			}
			stopwatch.Stop();
			Console.WriteLine($"Imported all data into SQL database in {stopwatch.Elapsed.TotalSeconds:F1} s");
		}

		private void ImportCompanies(IEnumerable<Ticker> tickers, NpgsqlConnection connection)
		{
			Console.WriteLine("Importing companies");
			TruncateTable("company", connection);
			using (var writer = connection.BeginBinaryImport("copy company (cik, symbol, name) from stdin binary"))
			{
				foreach (var ticker in tickers)
					writer.WriteRow(ticker.Cik, ticker.Symbol, ticker.Title);
				writer.Complete();
			}
		}

		private void ImportEarnings(IEnumerable<CompanyEarnings> companyEarnings, NpgsqlConnection connection)
		{
			Console.WriteLine("Importing earnings reports");
			TruncateTable("fact", connection);
			int i = 1;
			int count = companyEarnings.Count();
			foreach (var earnings in companyEarnings.OrderBy(x => x.Ticker.Symbol))
			{
				var ticker = earnings.Ticker;
				int cik = earnings.Ticker.Cik;
				PrintProgress(i, count, ticker);
				using (var writer = connection.BeginBinaryImport("copy fact (cik, name, end_date, value, fiscal_year, fiscal_period, form, filed, frame) from stdin binary"))
				{
					foreach (var fact in earnings.Facts.Values)
					{
						foreach (var pair in fact)
						{
							string factName = pair.Key;
							var factValues = pair.Value;
							writer.StartRow();
							writer.Write(earnings.Ticker.Cik);
							writer.Write(factName);
							writer.Write(factValues.End, NpgsqlDbType.Date);
							writer.Write(factValues.Value, NpgsqlDbType.Numeric);
							writer.Write(factValues.FiscalYear);
							writer.Write(factValues.FiscalPeriod, NpgsqlDbType.Char);
							writer.Write(factValues.Form);
							writer.Write(factValues.Filed, NpgsqlDbType.Date);
							writer.Write(factValues.Frame);
						}
					}
					writer.Complete();
				}
				i++;
			}
		}

		private void ImportPriceData(IEnumerable<Ticker> tickers, string priceDataDirectory, NpgsqlConnection connection)
		{
			TruncateTable("price", connection);
			int i = 1;
			int count = tickers.Count();
			foreach (var ticker in tickers.OrderBy(x => x.Symbol))
			{
				PrintProgress(i, count, ticker);
				var priceData = DataReader.GetPriceData(ticker.Symbol, priceDataDirectory);
				using (var writer = connection.BeginBinaryImport("copy price (cik, date, open, high, low, close, volume) from stdin binary"))
				{
					foreach (var pair in priceData)
					{
						var date = pair.Key;
						var price = pair.Value;
						writer.StartRow();
						writer.Write(ticker.Cik);
						writer.Write(date, NpgsqlDbType.Date);
						writer.Write(price.Open, NpgsqlDbType.Money);
						writer.Write(price.High, NpgsqlDbType.Money);
						writer.Write(price.Low, NpgsqlDbType.Money);
						writer.Write(price.Close, NpgsqlDbType.Money);
						writer.Write(price.Volume, NpgsqlDbType.Bigint);
					}
					writer.Complete();
				}
				i++;
			}
		}

		private void PrintProgress(int i, int count, Ticker ticker)
		{
			Console.WriteLine($"Processing {ticker.Symbol} ({i}/{count})");
		}

		private void TruncateTable(string table, NpgsqlConnection connection)
		{
			using (var command = new NpgsqlCommand($"truncate table {table} cascade", connection))
				command.ExecuteNonQuery();
		}
	}
}
