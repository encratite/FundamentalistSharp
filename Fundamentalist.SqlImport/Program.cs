using System.Reflection;

namespace Fundamentalist.CsvGenerator
{
	internal class Program
	{
		static void Main(string[] arguments)
		{
			if (arguments.Length != 6)
			{
				var assembly = Assembly.GetExecutingAssembly();
				var name = assembly.GetName();
				Console.WriteLine("Usage:");
				Console.WriteLine($"{name.Name} <path to companyfacts.zip> <price data directory> <path to company tickers> <profile data directory> <market cap directory> <CSV output directory>");
				return;
			}
			string companyFactsPath = arguments[0];
			string tickerPath = arguments[1];
			string priceDataDirectory = arguments[2];
			string profileDirectory = arguments[3];
			string marketCapDirectory = arguments[4];
			string csvOutputDirectory = arguments[5];
			var generator = new CsvGenerator();
			generator.WriteCsvFiles(companyFactsPath, tickerPath, priceDataDirectory, profileDirectory, marketCapDirectory, csvOutputDirectory);
		}
	}
}
