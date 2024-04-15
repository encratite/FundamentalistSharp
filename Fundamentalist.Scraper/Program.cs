using System.Reflection;

namespace Fundamentalist.Scraper
{
	internal static class Program
	{
		private static void Main(string[] arguments)
		{
			if (arguments.Length != 2)
			{
				var assembly = Assembly.GetExecutingAssembly();
				var name = assembly.GetName();
				Console.WriteLine("Usage:");
				Console.WriteLine($"{name.Name} <path to 10-Q .csv file> <price data directory to download files to>");
				return;
			}
			string csvPath = arguments[0];
			string directory = arguments[1];
			var scraper = new Scraper();
			scraper.Run(csvPath, directory);
		}
	}
}