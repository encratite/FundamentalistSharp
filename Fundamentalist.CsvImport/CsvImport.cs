using CsvHelper;
using CsvHelper.Configuration;
using Fundamentalist.Common;
using Fundamentalist.Common.Document;
using Fundamentalist.CsvImport.Csv;
using Fundamentalist.CsvImport.Document;
using MongoDB.Driver;
using System.Globalization;
using System.IO.Compression;

namespace Fundamentalist.CsvImport
{
	internal class CsvImport
	{
		private Configuration _configuration;
		private IMongoDatabase _database;

		public CsvImport(Configuration configuration)
		{
			_configuration = configuration;
		}

		public void Import()
		{
			_database = Utility.GetMongoDatabase(_configuration.ConnectionString);
			ImportSecData();
			ImportPriceData();
			ImportIndexPriceData();
			ImportIndexComponents();
			ImportTickers();
		}

		private void ImportSecData()
		{
			_database.DropCollection(Collection.Submissions);
			_database.CreateCollection(Collection.Submissions);
			var collection = _database.GetCollection<SecSubmission>(Collection.Submissions);
			var adshIndex = Builders<SecSubmission>.IndexKeys.Ascending(x => x.Form);
			collection.Indexes.CreateOne(new CreateIndexModel<SecSubmission>(adshIndex));
			var edgarFiles = Directory.GetFiles(_configuration.EdgarPath, "*.zip");
			foreach (string path in edgarFiles)
			{
				using var timer = new PerformanceTimer($"Processing {path}", "Processed archive");
				using var archive = ZipFile.OpenRead(path);
				var submissions = GetRecords<SubmissionRow>("sub.txt", archive);
				var numbers = GetRecords<NumberRow>("num.txt", archive);
				var secSubmissions = new Dictionary<string, SecSubmission>();
				foreach (var submission in submissions)
				{
					var secSubmission = submission.GetSecSubmission();
					secSubmissions[submission.Adsh] = secSubmission;
				}
				foreach (var number in numbers)
				{
					var secNumber = number.GetSecNumber();
					secSubmissions[number.Adsh].Numbers.Add(secNumber);
				}
				if (secSubmissions.Values.Any())
					collection.InsertMany(secSubmissions.Values);
			}
		}

		private void ImportPriceData()
		{
			using var timer = new PerformanceTimer("Importing price data", "Imported price data");
			_database.DropCollection(Collection.Prices);
			_database.CreateCollection(Collection.Prices);
			var collection = _database.GetCollection<Price>(Collection.Prices);
			var tickerIndex = Builders<Price>.IndexKeys.Ascending(x => x.Ticker);
			collection.Indexes.CreateOne(new CreateIndexModel<Price>(tickerIndex));
			var tickerDateIndex = Builders<Price>.IndexKeys.Ascending(x => x.Ticker).Ascending(x => x.Date);
			collection.Indexes.CreateOne(new CreateIndexModel<Price>(tickerDateIndex));
			using var reader = new StreamReader(_configuration.PriceCsvPath);
			using var csvReader = GetCsvReader(reader);
			var decimalConverter = new PriceDecimalConverter();
			var converterCache = csvReader.Context.TypeConverterCache;
			converterCache.AddConverter<decimal>(decimalConverter);
			converterCache.AddConverter<decimal?>(decimalConverter);
			var records = csvReader.GetRecords<PriceRow>();
			var batch = new List<Price>();
			foreach (var priceRow in records)
			{
				if (!priceRow.Volume.HasValue)
					continue;
				var priceData = priceRow.GetPrice();
				batch.Add(priceData);
				if (batch.Count >= 1000)
				{
					collection.InsertMany(batch);
					batch.Clear();
				}
			}
			if (batch.Any())
				collection.InsertMany(batch);
		}

		private void ImportIndexPriceData()
		{
			var collection = _database.GetCollection<Price>(Collection.Prices);
			using var reader = new StreamReader(_configuration.IndexPriceCsvPath);
			using var csvReader = GetCsvReader(reader);
			var records = csvReader.GetRecords<LegacyPriceRow>()
				.Select(row => row.GetPrice(null));
			collection.InsertMany(records);
		}

		private void ImportIndexComponents()
		{
			_database.DropCollection(Collection.IndexComponents);
			_database.CreateCollection(Collection.IndexComponents);
			var collection = _database.GetCollection<IndexComponents>(Collection.IndexComponents);
			var dateIndex = Builders<IndexComponents>.IndexKeys.Descending(x => x.Date);
			collection.Indexes.CreateOne(new CreateIndexModel<IndexComponents>(dateIndex));
			using var reader = new StreamReader(_configuration.IndexComponentsCsvPath);
			using var csvReader = GetCsvReader(reader);
			var records = csvReader.GetRecords<IndexComponentsRow>();
			// Eliminate duplicate dates, can't really deal with those anyway
			var documents = new Dictionary<DateTime, IndexComponents>();
			foreach (var record in records)
				documents[record.Date] = record.GetIndexComponents();
			collection.InsertMany(documents.Values);
		}

		private void ImportTickers()
		{
			using var timer = new PerformanceTimer("Importing ticker data", "Imported ticker data");
			_database.DropCollection(Collection.Tickers);
			_database.CreateCollection(Collection.Tickers);
			var collection = _database.GetCollection<TickerData>(Collection.Tickers);
			var cikIndex = Builders<TickerData>.IndexKeys.Ascending(x => x.Cik);
			collection.Indexes.CreateOne(new CreateIndexModel<TickerData>(cikIndex));
			using var reader = new StreamReader(_configuration.TickerCsvPath);
			using var csvReader = GetCsvReader(reader);
			var records = csvReader.GetRecords<TickerRow>();
			var usedSymbols = new HashSet<string>();
			var output = new List<TickerData>();
			foreach (var row in records)
			{
				var ticker = row.GetTickerData();
				if (
					usedSymbols.Contains(ticker.Ticker) ||
					ticker.Country != "US" ||
					!(
						ticker.Category == "Domestic Common Stock" ||
						ticker.Category == "Domestic Common Stock Primary Class" ||
						ticker.Category == "Domestic Common Stock Secondary Class"
					)
				)
					continue;
				output.Add(ticker);
				usedSymbols.Add(ticker.Ticker);
			}
			collection.InsertMany(output);
		}

		private List<T> GetRecords<T>(string filename, ZipArchive archive)
		{
			var sub = archive.GetEntry(filename);
			using var stream = sub.Open();
			using var reader = new StreamReader(stream);
			using var csvReader = GetCsvReader(reader, "\t");
			var records = csvReader.GetRecords<T>().ToList();
			return records;
		}

		private CsvReader GetCsvReader(StreamReader reader, string delimiter = ",")
		{
			var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
			{
				Delimiter = delimiter
			};
			var csvReader = new CsvReader(reader, configuration);
			return csvReader;
		}
	}
}
