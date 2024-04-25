using System.Reflection;

namespace Fundamentalist.Sql
{
	internal class Program
	{
		static void Main(string[] arguments)
		{
			if (arguments.Length != 4)
			{
				var assembly = Assembly.GetExecutingAssembly();
				var name = assembly.GetName();
				Console.WriteLine("Usage:");
				Console.WriteLine($"{name.Name} <path to directory containing XBLR JSON files> <price data directory> <path to company tickers> <PostgreSQL connection string>");
				return;
			}
			string xbrlDirectory = arguments[0];
			string tickerPath = arguments[1];
			string priceDataDirectory = arguments[2];
			string connectionString = arguments[3];
			var sqlImporter = new SqlImporter();
			sqlImporter.Import(xbrlDirectory, tickerPath, priceDataDirectory, connectionString);
		}
	}
}
