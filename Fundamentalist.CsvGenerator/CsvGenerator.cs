using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using Fundamentalist.CsvGenerator.Csv;
using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;

namespace Fundamentalist.CsvGenerator
{
	internal class CsvGenerator
	{
		public void WriteCsvFiles(string edgarPath, string csvOutputDirectory)
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			WriteSecData(edgarPath, csvOutputDirectory);
			stopwatch.Stop();
			Console.WriteLine($"Generated CSV files in {stopwatch.Elapsed.TotalSeconds:F1} s");
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
					var cache = context.TypeConverterOptionsCache;
					var options = new TypeConverterOptions[]
					{
						cache.GetOptions<DateOnly>(),
						cache.GetOptions<DateOnly?>(),
						cache.GetOptions<DateTime>()
					};
					foreach (var o in options)
						o.Formats = new string[] { "o" };
					write(csvWriter);
				}
			}
		}

		private void WriteSecData(string edgarPath, string csvOutputDirectory)
		{
			WriteCsv("sec_submission.csv", csvOutputDirectory, (submissionWriter) =>
			{
				submissionWriter.WriteHeader<SecSubmissionRow>();
				submissionWriter.NextRecord();
				WriteCsv("sec_number.csv", csvOutputDirectory, (numberWriter) =>
				{
					numberWriter.WriteHeader<SecNumberRow>();
					numberWriter.NextRecord();
					var edgarFiles = Directory.GetFiles(edgarPath, "*.zip");
					foreach (string path in edgarFiles)
					{
						Console.WriteLine($"Processing {path}");
						var stopwatch = new Stopwatch();
						stopwatch.Start();
						using var archive = ZipFile.OpenRead(path);
						var submissions = GetRecords<SubmissionRow>("sub.txt", archive);
						var numbers = GetRecords<NumberRow>("num.txt", archive);
						foreach (var submission in submissions)
						{
							var secSubmission = new SecSubmissionRow(submission);
							submissionWriter.WriteRecord(secSubmission);
							submissionWriter.NextRecord();
						}
						foreach (var number in numbers)
						{
							var secNumber = new SecNumberRow(number);
							numberWriter.WriteRecord(secNumber);
							numberWriter.NextRecord();
						}
						stopwatch.Stop();
						Console.WriteLine($"Processed archive in {stopwatch.Elapsed.TotalSeconds:F1} s");
					}
				});
			});
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
