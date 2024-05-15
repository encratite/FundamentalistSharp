using System.Reflection;

namespace Fundamentalist.CsvGenerator
{
	internal class Program
	{
		static void Main(string[] arguments)
		{
			if (arguments.Length != 7)
			{
				var assembly = Assembly.GetExecutingAssembly();
				var name = assembly.GetName();
				Console.WriteLine("Usage:");
				Console.WriteLine($"{name.Name} <path to companyfacts.zip> <path to EDGAR zip files> <price data directory> <path to company tickers> <profile data directory> <market cap directory> <CSV output directory>");
				return;
			}
			int offset = 0;
			Func<string> getArgument = () => arguments[offset++];
			string companyFactsPath = getArgument();
			string edgarPath = getArgument();
			string tickerPath = getArgument();
			string priceDataDirectory = getArgument();
			string profileDirectory = getArgument();
			string marketCapDirectory = getArgument();
			string csvOutputDirectory = getArgument();
			var generator = new CsvGenerator();
			generator.WriteCsvFiles(companyFactsPath, edgarPath, tickerPath, priceDataDirectory, profileDirectory, marketCapDirectory, csvOutputDirectory);
		}
	}
}
