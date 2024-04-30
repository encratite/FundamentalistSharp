using System.Reflection;

namespace Fundamentalist.Scraper
{
	internal static class Program
	{
		private static void Main(string[] arguments)
		{
			if (arguments.Length != 4)
			{
				var assembly = Assembly.GetExecutingAssembly();
				var name = assembly.GetName();
				Console.WriteLine("Usage:");
				Console.WriteLine($"{name.Name} <path to company_tickers.json> <price data .csv output directory> <profile .html output directory> <market cap output directory>");
				return;
			}
			string tickersPath = arguments[0];
			string priceDataDirectory = arguments[1];
			string profileDirectory = arguments[2];
			string marketCapDirectory = arguments[3];
			var scraper = new Scraper();
			scraper.Run(tickersPath, priceDataDirectory, profileDirectory, marketCapDirectory);
		}
	}
}