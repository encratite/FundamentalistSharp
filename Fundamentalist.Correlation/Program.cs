using System.Reflection;

namespace Fundamentalist.Correlation
{
	internal class Program
	{
		private static void Main(string[] arguments)
		{
			if (arguments.Length != 7)
			{
				var assembly = Assembly.GetExecutingAssembly();
				var name = assembly.GetName();
				Console.WriteLine("Usage:");
				Console.WriteLine($"{name.Name} <path to earnings .csv file> <price data directory> <forecast days> <number of features> <correlation output> <appearance output> <disappearance output>");
				return;
			}
			string earningsPath = arguments[0];
			string priceDataDirectory = arguments[1];
			int forecastDays = int.Parse(arguments[2]);
			int features = int.Parse(arguments[3]);
			string correlationOutput = arguments[4];
			string appearanceOutput = arguments[5];
			string disappearanceOutput = arguments[6];

			decimal minimumObservationRatio = 0.01m;
			DateTime fromDate = new DateTime(2010, 1, 1);
			DateTime toDate = new DateTime(2023, 1, 1);
			string logDirectory = null;

			var analyzer = new CorrelationAnalyzer(
				earningsPath,
				priceDataDirectory,
				features,
				minimumObservationRatio,
				fromDate,
				toDate,
				forecastDays,
				correlationOutput,
				appearanceOutput,
				disappearanceOutput
			);
			analyzer.Run();
		}
	}
}