using Fundamentalist.Common;
using Fundamentalist.Common.Json;
using HtmlAgilityPack;
using MySql.Data.MySqlClient;
using System.Data;
using System.Diagnostics;
using System.IO.Compression;
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

		public void Import(string companyFactsPath, string tickerPath, string priceDataDirectory, string profileDirectory, string marketCapDirectory, string connectionString)
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			using (var connection = new MySqlConnection(connectionString))
			{
				connection.Open();
				var tickers = DataReader.GetTickersFromJson(tickerPath);
				// ImportTickers(tickers, connection);
				ImportProfileData(profileDirectory, connection);
				ImportMarketCapData(marketCapDirectory, connection);
				ImportPriceData(tickers, priceDataDirectory, connection);
				ImportIndexPriceData(priceDataDirectory, connection);
				ImportEarnings(companyFactsPath, connection);
			}
			stopwatch.Stop();
			Console.WriteLine($"Imported all data into SQL database in {stopwatch.Elapsed.TotalSeconds:F1} s");
		}

		private void ImportTickers(List<Ticker> tickers, MySqlConnection connection)
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
			foreach (var ticker in tickers)
			{
				bool usedCik = usedCiks.Contains(ticker.Cik);
				bool exclude = usedCik || (ticker.Symbol.Contains("-") && !permittedSuffixes.IsMatch(ticker.Symbol)) || bannedTitlePattern.IsMatch(ticker.Title);
				table.Rows.Add(ticker.Symbol, ticker.Cik, ticker.Title, null, null, exclude);
				if (!usedCik)
					usedCiks.Add(ticker.Cik);
			}
			InsertDataTable(table, connection);
		}

		private string GetInsertQuery(DataTable table)
		{
			var columns = table.Columns.Cast<DataColumn>();
			string columnNames = string.Join(", ", columns.Select(x => x.ColumnName));
			string parameterNames = string.Join(", ", columns.Select(x => $"@{x.ColumnName}"));
			string query = $"insert into {table.TableName} ({columnNames}) values ({parameterNames})";
			return query;
		}

		private void InsertDataTable(DataTable table, MySqlConnection connection)
		{
			using (var adapter = new MySqlDataAdapter(string.Empty, connection))
			{
				string query = GetInsertQuery(table);
				adapter.InsertCommand = new MySqlCommand(query, connection);
				adapter.InsertCommand.UpdatedRowSource = UpdateRowSource.None;
				var insertParameters = adapter.InsertCommand.Parameters;
				foreach (DataColumn column in table.Columns)
				{
					MySqlDbType type;
					if (column.DataType == typeof(string))
						type = MySqlDbType.VarChar;
					else if (column.DataType == typeof(int))
						type = MySqlDbType.Int32;
					else if (column.DataType == typeof(long))
						type = MySqlDbType.Int64;
					else if (column.DataType == typeof(DateTime))
						type = MySqlDbType.Date;
					else if (column.DataType == typeof(bool))
						type = MySqlDbType.Bit;
					else
						throw new ApplicationException("Unable to map data type");
					var parameter = new MySqlParameter(column.ColumnName, type);
					parameter.SourceColumn = column.ColumnName;
					insertParameters.Add(parameter);
				}
				adapter.Update(table);
			}
		}

		private void ImportProfileData(string profileDirectory, MySqlConnection connection)
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
					string encodedText = nodes[i].InnerText;
					string output = HttpUtility.HtmlDecode(encodedText);
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
				using (var command = new MySqlCommand("update ticker set industry = @industry, sector = @sector where symbol = @symbol", connection))
				{
					var parameters = command.Parameters;
					parameters.AddWithValue("@industry", (object)industry ?? DBNull.Value);
					parameters.AddWithValue("@sector", (object)sector ?? DBNull.Value);
					parameters.AddWithValue("@symbol", symbol);
					command.ExecuteNonQuery();
				}
			}
		}

		private void ImportMarketCapData(string marketCapDirectory, MySqlConnection connection)
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

		private void ImportMarketCapSamples(string symbol, MarketCapSample[] samples, MySqlConnection connection)
		{
			// Console.WriteLine($"Importing market cap data for {symbol}");
			var table = new DataTable(MarketCapTable);
			table.Columns.AddRange(new DataColumn[]
			{
				new DataColumn("symbol", typeof(string)),
				new DataColumn("date", typeof(DateTime)),
				new DataColumn("market_cap", typeof(long)),
			});
			foreach (var sample in samples)
			{
				DateTime date = DateTimeOffset.FromUnixTimeMilliseconds(sample.Timestamp * 1000).UtcDateTime;
				table.Rows.Add(symbol, date, sample.MarketCap * 100000);
			}
			InsertDataTable(table, connection);
		}

		private void ImportEarnings(string companyFactsPath, MySqlConnection connection)
		{
			const int BatchSize = 100;
			Console.WriteLine("Importing earnings reports");
			TruncateTable(FactTable, connection);
			var batch = new List<CompanyFacts>();
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			using (var zipFile = ZipFile.OpenRead(companyFactsPath))
			{
				int offset = 0;
				int total = zipFile.Entries.Count;
				foreach (var entry in zipFile.Entries)
				{
					offset++;
					if (!HasPriceData(entry, connection))
						continue;
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
								Console.WriteLine($"Writing batch to database ({offset}/{total}, {offset / stopwatch.Elapsed.TotalSeconds:F2}/s)");
								ImportCompanyFacts(batch, connection);
								batch.Clear();
							}
						}
					}
				}
			}
			stopwatch.Stop();
		}

		private bool HasPriceData(ZipArchiveEntry entry, MySqlConnection connection)
		{
			var pattern = new Regex("[1-9][0-9]+");
			var match = pattern.Match(entry.Name);
			if (!match.Success)
				throw new ApplicationException("Unknown pattern in file name");
			int cik = int.Parse(match.Value);
			string query = @"
				select count(*)
				from
					ticker join price
					on ticker.symbol = price.symbol
				where ticker.cik = @cik";
			using (var command = new MySqlCommand(query, connection))
			{
				command.Parameters.AddWithValue("@cik", cik);
				int count = (int)command.ExecuteScalar();
				return count > 0;
			}
		}

		private void ImportCompanyFacts(List<CompanyFacts> batch, MySqlConnection connection)
		{
			var table = new DataTable(FactTable);
			table.Columns.AddRange(new DataColumn[]
			{
					new DataColumn("cik", typeof(int)),
					new DataColumn("name", typeof(string)),
					new DataColumn("unit", typeof(string)),
					new DataColumn("start_date", typeof(DateTime)),
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
									factValues.Start,
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
			InsertDataTable(table, connection);
		}

		private void ImportPriceData(IEnumerable<Ticker> tickers, string priceDataDirectory, MySqlConnection connection)
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

		private void ImportIndexPriceData(string priceDataDirectory, MySqlConnection connection)
		{
			Console.WriteLine("Importing index price data");
			var priceData = DataReader.GetPriceData(DataReader.IndexTicker, priceDataDirectory);
			ImportTickerPriceData(null, priceData, connection);
		}

		private void ImportTickerPriceData(string symbol, SortedList<DateTime, PriceData> priceData, MySqlConnection connection)
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
				InsertDataTable(table, connection);
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

		private void TruncateTable(string table, MySqlConnection connection)
		{
			using (var command = new MySqlCommand($"truncate table {table}", connection))
			{
				command.CommandTimeout = 3600;
				command.ExecuteNonQuery();
			}
		}
	}
}
