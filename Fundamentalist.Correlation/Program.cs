using System.Reflection;

namespace Fundamentalist.Correlation
{
	internal class Program
	{
		private static void Main(string[] arguments)
		{
			if (arguments.Length != 2)
			{
				var assembly = Assembly.GetExecutingAssembly();
				var name = assembly.GetName();
				Console.WriteLine("Usage:");
				Console.WriteLine($"{name.Name} <path to earnings .csv file> <price data directory to download files to>");
				return;
			}
			string earningsPath = arguments[0];
			string priceDataDirectory = arguments[1];

			int features = 1000;
			decimal minimumObservationRatio = 0.01m;
			DateTime fromDate = new DateTime(2010, 1, 1);
			DateTime toDate = new DateTime(2023, 1, 1);
			int forecastDays = 5;
			// string logDirectory = "Data";
			string logDirectory = null;

			var analyzer = new CorrelationAnalyzer(
				earningsPath,
				priceDataDirectory,
				features,
				minimumObservationRatio,
				fromDate,
				toDate,
				forecastDays,
				logDirectory
			);
			analyzer.Run();
		}
	}
}