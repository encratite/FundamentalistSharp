using CsvHelper;
using CsvHelper.Configuration;
using Fundamentalist.Common;
using Fundamentalist.Common.Document;
using Fundamentalist.CsvImport.Csv;
using Fundamentalist.CsvImport.Document;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using System.Globalization;
using System.IO.Compression;

namespace Fundamentalist.CsvImport
{
	internal class CsvImport
	{
		private const string SubmissionsCollection = "submissions";
		private const string PricesCollection = "prices";
		private const string TickersCollection = "tickers";

		private const int PriceMinimumYear = 2009;

		public void ImportCsvFiles(string edgarPath, string priceCsvPath, string indexCsvPath, string tickerCsvPath, string connectionString)
		{
			var pack = new ConventionPack
			{
				new CamelCaseElementNameConvention()
			};
			ConventionRegistry.Register(nameof(CamelCaseElementNameConvention), pack, _ => true);
			BsonSerializer.RegisterSerializer(typeof(decimal), new DecimalSerializer(BsonType.Decimal128));
			BsonSerializer.RegisterSerializer(typeof(decimal?), new NullableSerializer<decimal>(new DecimalSerializer(BsonType.Decimal128)));
			var client = new MongoClient(connectionString);
			var database = client.GetDatabase("fundamentalist");
			ImportSecData(edgarPath, database);
			ImportPriceData(priceCsvPath, database);
			ImportIndexData(indexCsvPath, database);
			ImportTickers(tickerCsvPath, database);
		}

		private void ImportSecData(string edgarPath, IMongoDatabase database)
		{
			database.DropCollection(SubmissionsCollection);
			database.CreateCollection(SubmissionsCollection);
			var collection = database.GetCollection<SecSubmission>(SubmissionsCollection);
			var adshIndex = Builders<SecSubmission>.IndexKeys.Ascending(x => x.Form);
			collection.Indexes.CreateOne(new CreateIndexModel<SecSubmission>(adshIndex));
			var edgarFiles = Directory.GetFiles(edgarPath, "*.zip");
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

		private void ImportPriceData(string priceCsvPath, IMongoDatabase database)
		{
			using var timer = new PerformanceTimer("Importing price data", "Imported price data");
			database.DropCollection(PricesCollection);
			database.CreateCollection(PricesCollection);
			var collection = database.GetCollection<Price>(PricesCollection);
			var tickerDateIndex = Builders<Price>.IndexKeys.Ascending(x => x.Ticker).Ascending(x => x.Date);
			collection.Indexes.CreateOne(new CreateIndexModel<Price>(tickerDateIndex));
			using var reader = new StreamReader(priceCsvPath);
			using var csvReader = GetCsvReader(reader);
			var decimalConverter = new PriceDecimalConverter();
			var converterCache = csvReader.Context.TypeConverterCache;
			converterCache.AddConverter<decimal>(decimalConverter);
			converterCache.AddConverter<decimal?>(decimalConverter);
			var records = csvReader.GetRecords<PriceRow>();
			var batch = new List<Price>();
			foreach (var priceRow in records)
			{
				if (priceRow.Date.Year < PriceMinimumYear || !priceRow.Volume.HasValue)
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

		private void ImportIndexData(string indexCsvPath, IMongoDatabase database)
		{
			var collection = database.GetCollection<Price>(PricesCollection);
			using var reader = new StreamReader(indexCsvPath);
			using var csvReader = GetCsvReader(reader);
			var records = csvReader.GetRecords<LegacyPriceRow>()
				.Where(row => row.Date.Year >= PriceMinimumYear)
				.Select(row => row.GetPrice(null));
			collection.InsertMany(records);
		}

		private void ImportTickers(string tickerCsvPath, IMongoDatabase database)
		{
			using var timer = new PerformanceTimer("Importing ticker data", "Imported ticker data");
			database.DropCollection(TickersCollection);
			database.CreateCollection(TickersCollection);
			var collection = database.GetCollection<TickerData>(TickersCollection);
			var cikIndex = Builders<TickerData>.IndexKeys.Ascending(x => x.Cik);
			collection.Indexes.CreateOne(new CreateIndexModel<TickerData>(cikIndex));
			using var reader = new StreamReader(tickerCsvPath);
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
