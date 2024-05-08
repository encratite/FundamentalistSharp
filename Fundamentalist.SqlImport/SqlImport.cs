using Fundamentalist.Common;
using Fundamentalist.Common.Json;
using HtmlAgilityPack;
using System;
using System.Collections.Concurrent;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;

namespace Fundamentalist.SqlImport
{
	internal class SqlImport
	{
		const string TickerTable = "ticker";
		const string FactTable = "fact";
		const string PriceTable = "price";
		const string MarketCapTable = "market_cap";

		public void Import(string companyFactsPath, string tickerPath, string priceDataDirectory, string profileDirectory, string marketCapDirectory, string connectionString)
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			using (var connection = new SqlConnection(connectionString))
			{
				connection.Open();
				var tickers = DataReader.GetTickersFromJson(tickerPath);
				// ImportTickers(tickers, connection);
				// ImportProfileData(profileDirectory, connection);
				// ImportMarketCapData(marketCapDirectory, connection);
				// ImportEarnings(companyFactsPath, connection);
				// ImportPriceData(tickers, priceDataDirectory, connection);
				// ImportIndexPriceData(priceDataDirectory, connection);
				AddMissingUnits(companyFactsPath, connection);
			}
			stopwatch.Stop();
			Console.WriteLine($"Imported all data into SQL database in {stopwatch.Elapsed.TotalSeconds:F1} s");
		}

		private void ImportTickers(List<Ticker> tickers, SqlConnection connection)
		{
			Console.WriteLine("Importing tickers");
			TruncateTable(TickerTable, connection);
			var table = new DataTable(TickerTable);
			table.Columns.AddRange(new DataColumn[]
			{
				new DataColumn("symbol", typeof(string)),
				new DataColumn("cik", typeof(int)),
				new DataColumn("company", typeof(string)),
				new DataColumn("sector", typeof(string)),
				new DataColumn("industry", typeof(string)),
				new DataColumn("exclude", typeof(bool))
			});
			var usedCiks = new HashSet<int>();
			var permittedSuffixes = new Regex("-(A|B|C|PA|PB|PC)$");
			var bannedTitlePattern = new Regex(" ETF| Fund|S&P 500");
			foreach (var ticker in tickers.Take(1))
			{
				bool usedCik = usedCiks.Contains(ticker.Cik);
				bool exclude = usedCik || (ticker.Symbol.Contains("-") && !permittedSuffixes.IsMatch(ticker.Symbol)) || bannedTitlePattern.IsMatch(ticker.Title);
				table.Rows.Add(ticker.Symbol, ticker.Cik, ticker.Title, DBNull.Value, DBNull.Value, exclude);
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
				// Console.WriteLine($"Setting industry and sector of {symbol}");
				var document = new HtmlDocument();
				document.Load(file);
				var nodes = document.DocumentNode.SelectNodes("//div[@class='json_box']/div[2]/div/span");
				if (nodes == null || nodes.Count < 19)
				{
					// Utility.WriteError($"Unable to determine industry and sector of {symbol}");
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
			// Console.WriteLine($"Importing market cap data for {symbol}");
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

		private void ImportEarnings(string companyFactsPath, SqlConnection connection)
		{
			const int BatchSize = 100;
			Console.WriteLine("Importing earnings reports");
			TruncateTable(FactTable, connection);
			var batch = new List<CompanyFacts>();
			int progress = 0;
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			using (var zipFile = ZipFile.OpenRead(companyFactsPath))
			{
				int offset = 1;
				int total = zipFile.Entries.Count;
				foreach (var entry in zipFile.Entries)
				{
					using (var stream = entry.Open())
					{
						using (var streamReader = new StreamReader(stream))
						{
							string json = streamReader.ReadToEnd();
							var options = new JsonSerializerOptions
							{
								PropertyNameCaseInsensitive = true
							};
							var companyFacts = JsonSerializer.Deserialize<CompanyFacts>(json, options);
							batch.Add(companyFacts);
							if (batch.Count >= BatchSize || offset == total)
							{
								progress += batch.Count;
								Console.WriteLine($"Writing batch to database ({progress}/{total}, {progress / stopwatch.Elapsed.TotalSeconds:F2}/s)");
								ImportCompanyFacts(batch, connection);
								batch.Clear();
							}
						}
					}
					offset++;
				}
			}
			stopwatch.Stop();
		}

		private void ImportCompanyFacts(List<CompanyFacts> batch, SqlConnection connection)
		{
			using (var bulkCopy = GetBulkCopy(connection))
			{
				var table = new DataTable(FactTable);
				table.Columns.AddRange(new DataColumn[]
				{
						new DataColumn("cik", typeof(int)),
						new DataColumn("name", typeof(string)),
						new DataColumn("unit", typeof(string)),
						new DataColumn("end_date", typeof(DateTime)),
						new DataColumn("value", typeof(decimal)),
						new DataColumn("fiscal_year", typeof(int)),
						new DataColumn("fiscal_period", typeof(string)),
						new DataColumn("form", typeof(string)),
						new DataColumn("filed", typeof(DateTime)),
						new DataColumn("frame", typeof(string)),
				});
				foreach (var companyFacts in batch)
				{
					if (companyFacts.Cik == 0 || companyFacts.Facts == null)
						continue;
					foreach (var fact in companyFacts.Facts)
					{
						foreach (var pair in fact.Value)
						{
							string factName = pair.Key;
							foreach (var unitPair in pair.Value.Units)
							{
								foreach (var factValues in unitPair.Value)
								{
									table.Rows.Add(
										companyFacts.Cik,
										factName,
										unitPair.Key,
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
						}
					}
				}
				bulkCopy.BulkCopyTimeout = 3600;
				bulkCopy.DestinationTableName = FactTable;
				bulkCopy.WriteToServer(table);
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
			{
				command.CommandTimeout = 3600;
				command.ExecuteNonQuery();
			}
		}

		private SqlBulkCopy GetBulkCopy(SqlConnection connection)
		{
			var options = SqlBulkCopyOptions.TableLock | SqlBulkCopyOptions.UseInternalTransaction;
			return new SqlBulkCopy(connection, options, null);
		}

		private void AddMissingUnits(string companyFactsPath, SqlConnection connection)
		{
			Console.WriteLine("Adding missing units");
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			using (var zipFile = ZipFile.OpenRead(companyFactsPath))
			{
				int offset = 1;
				int total = zipFile.Entries.Count;
				foreach (var entry in zipFile.Entries)
				{
					using (var stream = entry.Open())
					{
						using (var streamReader = new StreamReader(stream))
						{
							string json = streamReader.ReadToEnd();
							var options = new JsonSerializerOptions
							{
								PropertyNameCaseInsensitive = true
							};
							var companyFacts = JsonSerializer.Deserialize<CompanyFacts>(json, options);
							AddMissingUnit(companyFacts, connection);
						}
					}
					Console.WriteLine($"Added missing units ({offset}/{zipFile.Entries.Count}, {offset / stopwatch.Elapsed.TotalSeconds:F2}/s)");
					offset++;
				}
			}
			stopwatch.Stop();
		}

		private void AddMissingUnit(CompanyFacts companyFacts, SqlConnection connection)
		{
			if (companyFacts.Cik == 0 || companyFacts.Facts == null)
				return;
			foreach (var fact in companyFacts.Facts)
			{
				foreach (var pair in fact.Value)
				{
					string factName = pair.Key;
					foreach (var unitPair in pair.Value.Units)
					{
						foreach (var factValues in unitPair.Value)
						{
							string query = "update fact set unit = @unit where cik = @cik and filed = @filed and name = @name";
							using (var command = new SqlCommand(query, connection))
							{
								var parameters = command.Parameters;
								parameters.AddWithValue("@unit", unitPair.Key);
								parameters.AddWithValue("@cik", companyFacts.Cik);
								parameters.AddWithValue("@filed", factValues.Filed);
								parameters.AddWithValue("@name", factName);
								command.ExecuteNonQuery();
							}
						}
					}
				}
			}
		}
	}
}
