﻿using Fundamentalist.Common;
using Fundamentalist.Common.Json;
using HtmlAgilityPack;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;

namespace Fundamentalist.SqlImport
{
	internal class SqlImport
	{
		const string TickerTable = "ticker";
		const string FactTable = "fact";
		const string PriceTable = "price";
		const string MarketCapTable = "market_cap";

		public void Import(string xbrlDirectory, string tickerPath, string priceDataDirectory, string profileDirectory, string marketCapDirectory, string connectionString)
		{
			// var xbrlParser = new XbrlParser();
			// xbrlParser.Load(xbrlDirectory, tickerPath, priceDataDirectory);
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			using (var connection = new SqlConnection(connectionString))
			{
				connection.Open();
				// ImportTickers(xbrlParser.Tickers, connection);
				// ImportProfileData(profileDirectory, connection);
				ImportMarketCapData(marketCapDirectory, connection);
				// ImportEarnings(xbrlParser.Earnings, connection);
				// ImportPriceData(xbrlParser.Tickers, priceDataDirectory, connection);
				// ImportIndexPriceData(priceDataDirectory, connection);
			}
			stopwatch.Stop();
			Console.WriteLine($"Imported all data into SQL database in {stopwatch.Elapsed.TotalSeconds:F1} s");
		}

		private void ImportTickers(IEnumerable<Ticker> tickers, SqlConnection connection)
		{
			Console.WriteLine("Importing tickers");
			TruncateTable(TickerTable, connection);
			var table = new DataTable(TickerTable);
			table.Columns.AddRange(new DataColumn[]
			{
				new DataColumn("symbol", typeof(string)),
				new DataColumn("cik", typeof(int)),
				new DataColumn("company", typeof(string)),
				new DataColumn("exclude", typeof(bool))
			});
			var usedCiks = new HashSet<int>();
			var permittedSuffixes = new Regex("-(A|B|C|PA|PB|PC)$");
			var bannedTitlePattern = new Regex(" ETF| Fund|S&P 500");
			foreach (var ticker in tickers)
			{
				bool usedCik = usedCiks.Contains(ticker.Cik);
				bool exclude = usedCik || (ticker.Symbol.Contains("-") && !permittedSuffixes.IsMatch(ticker.Symbol)) || bannedTitlePattern.IsMatch(ticker.Title);
				table.Rows.Add(ticker.Symbol, ticker.Cik, ticker.Title, exclude);
				if (!usedCik)
					usedCiks.Add(ticker.Cik);
			}
			using (var bulkCopy = GetBulkCopy(connection))
			{
				bulkCopy.DestinationTableName = TickerTable;
				bulkCopy.WriteToServer(table);
			}
		}

		private void ImportProfileData(string profileDirectory, SqlConnection connection)
		{
			var files = Directory.GetFiles(profileDirectory, "*.html");
			foreach (string file in files)
			{
				string symbol = Path.GetFileNameWithoutExtension(file);
				Console.WriteLine($"Setting industry and sector of {symbol}");
				var document = new HtmlDocument();
				document.Load(file);
				var nodes = document.DocumentNode.SelectNodes("//div[@class='json_box']/div[2]/div/span");
				if (nodes == null || nodes.Count < 19)
				{
					Utility.WriteError($"Unable to determine industry and sector of {symbol}");
					continue;
				}
				var getText = (int i) =>
				{
					string encodedTExt = nodes[i].InnerText;
					string output = HttpUtility.HtmlDecode(encodedTExt);
					if (output.Length >= 2 && output[0] == '\'' && output[output.Length - 1] == '\'')
						output = output.Substring(1, output.Length - 2);
					if (output.Length == 0 || output == "NULL")
						output = null;
					else
						output = output.Trim();
					return output;
				};
				string industry = getText(17);
				string sector = getText(18);
				using (var command = new SqlCommand("update ticker set industry = @industry, sector = @sector where symbol = @symbol", connection))
				{
					var parameters = command.Parameters;
					parameters.AddWithValue("@industry", (object)industry ?? DBNull.Value);
					parameters.AddWithValue("@sector", (object)sector ?? DBNull.Value);
					parameters.AddWithValue("@symbol", symbol);
					command.ExecuteNonQuery();
				}
			}
		}

		private void ImportMarketCapData(string marketCapDirectory, SqlConnection connection)
		{
			Console.WriteLine("Importing market cap data");
			TruncateTable(MarketCapTable, connection);
			var pattern = new Regex("data = (\\[\\{\"d\".+?\\}\\]);", RegexOptions.Multiline);
			var paths = Directory.GetFiles(marketCapDirectory, "*.html");
			foreach (string path in paths)
			{
				if (path.Contains("Index-"))
					continue;
				string symbol = Path.GetFileNameWithoutExtension(path);
				string html = File.ReadAllText(path);
				var match = pattern.Match(html);
				if (!match.Success)
				{
					Utility.WriteError($"Failed to extract JavaScript from {path}");
					continue;
				}
				string json = match.Groups[1].Value;
				var samples = JsonSerializer.Deserialize<MarketCapSample[]>(json);
				ImportMarketCapSamples(symbol, samples, connection);
			}
		}

		private void ImportMarketCapSamples(string symbol, MarketCapSample[] samples, SqlConnection connection)
		{
			Console.WriteLine($"Importing market cap data for {symbol}");
			using (var bulkCopy = GetBulkCopy(connection))
			{
				var table = new DataTable(MarketCapTable);
				table.Columns.AddRange(new DataColumn[]
				{
						new DataColumn("symbol", typeof(string)),
						new DataColumn("date", typeof(DateTime)),
						new DataColumn("value", typeof(int)),
				});
				foreach (var sample in samples)
				{
					DateTime date = DateTimeOffset.FromUnixTimeMilliseconds(sample.Timestamp * 1000).UtcDateTime;
					table.Rows.Add(symbol, date, sample.MarketCap);
				}
				bulkCopy.DestinationTableName = MarketCapTable;
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
				using (var bulkCopy = GetBulkCopy(connection))
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
				if (priceData != null)
					ImportTickerPriceData(ticker.Symbol, priceData, connection);
				i++;
			}
		}

		private void ImportIndexPriceData(string priceDataDirectory, SqlConnection connection)
		{
			Console.WriteLine("Importing index price data");
			var priceData = DataReader.GetPriceData(DataReader.IndexTicker, priceDataDirectory);
			ImportTickerPriceData(null, priceData, connection);
		}

		private void ImportTickerPriceData(string symbol, SortedList<DateTime, PriceData> priceData, SqlConnection connection)
		{
			var table = new DataTable(PriceTable);
			table.Columns.AddRange(new DataColumn[]
			{
					new DataColumn("symbol", typeof(string)),
					new DataColumn("date", typeof(DateTime)),
					new DataColumn("open_price", typeof(decimal)),
					new DataColumn("high", typeof(decimal)),
					new DataColumn("low", typeof(decimal)),
					new DataColumn("close_price", typeof(decimal)),
					new DataColumn("adjusted_close", typeof(decimal)),
					new DataColumn("volume", typeof(long)),
			});
			try
			{
				using (var bulkCopy = GetBulkCopy(connection))
				{
					foreach (var pair in priceData)
					{
						var date = pair.Key;
						var price = pair.Value;
						table.Rows.Add(
							symbol,
							date,
							price.Open,
							price.High,
							price.Low,
							price.Close,
							price.AdjustedClose,
							price.Volume
						);
					}
					bulkCopy.DestinationTableName = PriceTable;
					bulkCopy.WriteToServer(table);
				}
			}
			catch (OverflowException)
			{
				Utility.WriteError("Failed to write data to database due to an arithmetic overflow");
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