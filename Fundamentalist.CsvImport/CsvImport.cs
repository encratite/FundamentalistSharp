using CsvHelper;
using CsvHelper.Configuration;
using Fundamentalist.Common;
using Fundamentalist.CsvGenerator.Csv;
using Fundamentalist.CsvGenerator.Document;
using MongoDB.Bson.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using System.Globalization;
using System.IO.Compression;
using Fundamentalist.CsvImport.Csv;
using Fundamentalist.CsvImport.Document;
using System.ComponentModel;
using Fundamentalist.CsvImport;

namespace Fundamentalist.CsvGenerator
{
	internal class CsvImport
	{
		public void ImportCsvFiles(string edgarPath, string priceCsvPath, string connectionString)
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
			// ImportSecData(edgarPath, database);
			ImportPriceData(priceCsvPath, database);
		}

		private void ImportSecData(string edgarPath, IMongoDatabase database)
		{
			const string CollectionName = "submissions";
			database.DropCollection(CollectionName);
			database.CreateCollection(CollectionName);
			var collection = database.GetCollection<SecSubmission>(CollectionName);
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
					var secSubmission = new SecSubmission(submission);
					secSubmissions[submission.Adsh] = secSubmission;
				}
				foreach (var number in numbers)
				{
					var secNumber = new SecNumber(number);
					secSubmissions[number.Adsh].Numbers.Add(secNumber);
				}
				if (secSubmissions.Values.Any())
					collection.InsertMany(secSubmissions.Values);
			}
		}

		private void ImportPriceData(string priceCsvPath, IMongoDatabase database)
		{
			using var timer = new PerformanceTimer("Importing price data", "Imported price data");
			const string CollectionName = "prices";
			database.DropCollection(CollectionName);
			database.CreateCollection(CollectionName);
			var collection = database.GetCollection<Price>(CollectionName);
			var tickerDateIndex = Builders<Price>.IndexKeys.Ascending(x => x.Ticker).Ascending(x => x.Date);
			collection.Indexes.CreateOne(new CreateIndexModel<Price>(tickerDateIndex));
			using var reader = new StreamReader(priceCsvPath);
			var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
			{
				Delimiter = ","
			};
			using var csvReader = new CsvReader(reader, configuration);
			var decimalConverter = new PriceDecimalConverter();
			var converterCache = csvReader.Context.TypeConverterCache;
			converterCache.AddConverter<decimal>(decimalConverter);
			converterCache.AddConverter<decimal?>(decimalConverter);
			var records = csvReader.GetRecords<PriceRow>();
			var batch = new List<Price>();
			foreach (var priceRow in records)
			{
				if (priceRow.Date.Year < 2009 || !priceRow.Volume.HasValue)
					continue;
				var priceData = new Price(priceRow);
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
	}
}
