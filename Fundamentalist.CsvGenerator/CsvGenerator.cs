using CsvHelper;
using CsvHelper.Configuration;
using Fundamentalist.Common;
using Fundamentalist.Common.Json;
using Fundamentalist.CsvGenerator.Csv;
using HtmlAgilityPack;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;

namespace Fundamentalist.CsvGenerator
{
	internal class CsvGenerator
	{
		private HashSet<int> _usedCiks;

		public void WriteCsvFiles(string companyFactsPath, string edgarPath, string tickerPath, string priceDataDirectory, string profileDirectory, string marketCapDirectory, string csvOutputDirectory)
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			_usedCiks = new HashSet<int>();
			// var tickers = DataReader.GetTickersFromJson(tickerPath);
			// WriteTickers(tickers, profileDirectory, csvOutputDirectory);
			// WriteMarketCapData(marketCapDirectory, csvOutputDirectory);
			// WritePriceData(tickers, priceDataDirectory, csvOutputDirectory);
			// WriteEarnings(companyFactsPath, csvOutputDirectory);
			WriteOldEarnings(edgarPath, csvOutputDirectory);
			stopwatch.Stop();
			Console.WriteLine($"Generated CSV files in {stopwatch.Elapsed.TotalSeconds:F1} s");
		}

		private void WriteTickers(List<Ticker> tickers, string profileDirectory, string csvOutputDirectory)
		{
			Console.WriteLine("Writing ticker data");
			var companyProfiles = GetCompanyProfiles(profileDirectory);
			var usedCiks = new HashSet<int>();
			var permittedSuffixes = new Regex("-(A|B|C|PA|PB|PC)$");
			var bannedTitlePattern = new Regex(" ETF| Fund|S&P 500");
			WriteCsv("ticker.csv", csvOutputDirectory, csvWriter =>
			{
				csvWriter.WriteHeader<TickerRow>();
				csvWriter.NextRecord();
				foreach (var ticker in tickers)
				{
					bool usedCik = usedCiks.Contains(ticker.Cik);
					bool exclude = usedCik || (ticker.Symbol.Contains("-") && !permittedSuffixes.IsMatch(ticker.Symbol)) || bannedTitlePattern.IsMatch(ticker.Title);
					if (!usedCik)
						usedCiks.Add(ticker.Cik);
					IndustrySector industrySector;
					companyProfiles.TryGetValue(ticker.Symbol, out industrySector);
					var tickerRow = new TickerRow
					{
						Symbol = GetNullString(ticker.Symbol),
						Cik = ticker.Cik,
						Company = GetNullString(ticker.Title),
						Industry = GetNullString(industrySector?.Industry),
						Sector = GetNullString(industrySector?.Sector),
						Exclude = exclude ? 1 : 0
					};
					csvWriter.WriteRecord(tickerRow);
					csvWriter.NextRecord();
				}
			});
		}

		private void WriteCsv(string fileName, string csvOutputDirectory, Action<CsvWriter> write)
		{
			string path = Path.Combine(csvOutputDirectory, fileName);
			using (var writer = new StreamWriter(path))
			{
				using (var csvWriter = new CsvWriter(writer, CultureInfo.InvariantCulture))
				{
					var context = csvWriter.Context;
					context.Configuration.NewLine = "\n";
					var options = context.TypeConverterOptionsCache.GetOptions<DateOnly>();
					options.Formats = new string[] { "o" };
					write(csvWriter);
				}
			}
		}

		private Dictionary<string, IndustrySector> GetCompanyProfiles(string profileDirectory)
		{
			var output = new Dictionary<string, IndustrySector>();
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
				output[symbol] = new IndustrySector(industry, sector);
			}
			return output;
		}

		private void WriteMarketCapData(string marketCapDirectory, string csvOutputDirectory)
		{
			Console.WriteLine("Writing market cap data");
			var pattern = new Regex("data = (\\[\\{\"d\".+?\\}\\]);", RegexOptions.Multiline);
			var paths = Directory.GetFiles(marketCapDirectory, "*.html");
			WriteCsv("market_cap.csv", csvOutputDirectory, csvWriter =>
			{
				csvWriter.WriteHeader<MarketCapRow>();
				csvWriter.NextRecord();
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
					foreach (var sample in samples)
					{
						var date = DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeMilliseconds(sample.Timestamp * 1000).UtcDateTime);
						var row = new MarketCapRow
						{
							Symbol = GetNullString(symbol),
							Date = date,
							MarketCap = sample.MarketCap * 100000
						};
						csvWriter.WriteRecord(row);
						csvWriter.NextRecord();
					}
				}
			});
		}

		private void WriteEarnings(string companyFactsPath, string csvOutputDirectory)
		{
			Console.WriteLine("Writing earnings reports");
			var batch = new List<CompanyFacts>();
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			using (var zipFile = ZipFile.OpenRead(companyFactsPath))
			{
				WriteCsv("fact.csv", csvOutputDirectory, csvWriter =>
				{
					csvWriter.WriteHeader<TickerRow>();
					csvWriter.NextRecord();
					foreach (var entry in zipFile.Entries)
					{
						var pattern = new Regex("[1-9][0-9]+");
						var match = pattern.Match(entry.Name);
						if (!match.Success)
							throw new ApplicationException("Unknown pattern in file name");
						int cik = int.Parse(match.Value);
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
								WriteCompanyFacts(companyFacts, csvWriter);
							}
						}
						_usedCiks.Add(cik);
					}
				});
			}
			stopwatch.Stop();
		}

		private void WriteOldEarnings(string edgarPath, string csvOutputDirectory)
		{
			var edgarFiles = Directory.GetFiles(edgarPath, "*.zip");
			foreach (string path in edgarFiles.OrderDescending())
			{
				using var archive = ZipFile.OpenRead(path);
				var accessionMap = ParseSub(archive);
				ParseNum(archive);
			}
		}

		private List<T> GetRecords<T>(string filename, ZipArchive archive)
		{
			var sub = archive.GetEntry(filename);
			using var stream = sub.Open();
			using var reader = new StreamReader(stream);
			var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
			{
				Delimiter = "\t"
			};
			using var csvReader = new CsvReader(reader, configuration);
			var records = csvReader.GetRecords<T>().ToList();
			return records;
		}

		private Dictionary<string, SubRow> ParseSub(ZipArchive archive)
		{
			var records = GetRecords<SubRow>("sub.txt", archive);
			var accessionMap = new Dictionary<string, SubRow>();
			foreach (var record in records)
				accessionMap[record.AccessionNumber] = record;
			return accessionMap;
		}

		private void ParseNum(ZipArchive archive)
		{
			var records = GetRecords<NumRow>("num.txt", archive);
		}

		private void WriteCompanyFacts(CompanyFacts companyFacts, CsvWriter csvWriter)
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
							var row = new FactRow
							{
								Cik = companyFacts.Cik,
								Name = GetNullString(factName),
								Unit = GetNullString(unitPair.Key),
								Start = factValues.Start,
								End = factValues.End,
								Value = factValues.Value,
								FiscalYear = factValues.FiscalYear,
								FiscalPeriod = GetNullString(factValues.FiscalPeriod),
								Form = GetNullString(factValues.Form),
								Filed = factValues.Filed,
								Frame = GetNullString(factValues.Frame)
							};
							csvWriter.WriteRecord(row);
							csvWriter.NextRecord();
						}
					}
				}
			}
		}

		private void WritePriceData(IEnumerable<Ticker> tickers, string priceDataDirectory, string csvOutputDirectory)
		{
			Console.WriteLine("Writing price data");
			WriteCsv("price.csv", csvOutputDirectory, csvWriter =>
			{
				csvWriter.WriteHeader<PriceRow>();
				csvWriter.NextRecord();
				var indexPriceData = DataReader.GetPriceData(DataReader.IndexTicker, priceDataDirectory);
				WriteTickerPriceData(null, indexPriceData, csvWriter);
				foreach (var ticker in tickers.OrderBy(x => x.Symbol))
				{
					var priceData = DataReader.GetPriceData(ticker.Symbol, priceDataDirectory);
					if (priceData != null)
						WriteTickerPriceData(ticker.Symbol, priceData, csvWriter);
				}
			});
		}

		private void WriteTickerPriceData(string symbol, SortedList<DateOnly, PriceData> priceData, CsvWriter csvWriter)
		{
			foreach (var pair in priceData)
			{
				var date = pair.Key;
				var price = pair.Value;
				var row = new PriceRow
				{
					Symbol = GetNullString(symbol),
					Date = date,
					Open = price.Open,
					High = price.High,
					Low = price.Low,
					Close = price.Close,
					AdjustedClose = price.AdjustedClose,
					Volume = price.Volume
				};
				csvWriter.WriteRecord(row);
				csvWriter.NextRecord();
			}
		}

		private string GetNullString(string input)
		{
			return input ?? "\\N";
		}
	}
}
