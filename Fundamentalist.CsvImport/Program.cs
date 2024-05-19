using System.Reflection;

namespace Fundamentalist.CsvImport
{
	internal class Program
	{
		static void Main(string[] arguments)
		{
			if (arguments.Length != 5)
			{
				var assembly = Assembly.GetExecutingAssembly();
				var name = assembly.GetName();
				Console.WriteLine("Usage:");
				Console.WriteLine($"{name.Name} <path to EDGAR zip files> <Sharadar price .csv> <Yahoo Finance index price data .csv> <Sharader ticker .csv> <MongoDB connection string>");
				return;
			}
			int offset = 0;
			Func<string> getArgument = () => arguments[offset++];
			string edgarPath = getArgument();
			string priceCsvPath = getArgument();
			string indexCsvPath = getArgument();
			string tickerCsvPath = getArgument();
			string connectionString = getArgument();
			var import = new CsvImport();
			import.ImportCsvFiles(edgarPath, priceCsvPath, indexCsvPath, tickerCsvPath, connectionString);
		}
	}
}
