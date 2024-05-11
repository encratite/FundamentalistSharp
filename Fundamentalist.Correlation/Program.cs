using System.Reflection;

namespace Fundamentalist.Correlation
{
	internal class Program
	{
		private static void Main(string[] arguments)
		{
			if (arguments.Length != 11)
			{
				var assembly = Assembly.GetExecutingAssembly();
				var name = assembly.GetName();
				Console.WriteLine("Usage:");
				Console.WriteLine($"{name.Name} <path to earnings .csv file> <price data directory> <forecast days> <number of features> <nominal correlation output> <relative correlation output> <presence output> <appearance output> <disappearance output> <feature count output> <weekday performance output>");
				return;
			}
			string earningsPath = arguments[0];
			string priceDataDirectory = arguments[1];
			int forecastDays = int.Parse(arguments[2]);
			int features = int.Parse(arguments[3]);
			string nominalCorrelationOutput = arguments[4];
			string relativeCorrelationOutput = arguments[5];
			string presenceOutput = arguments[6];
			string appearanceOutput = arguments[7];
			string disappearanceOutput = arguments[8];
			string featureCountOutput = arguments[9];
			string weekdayOutput = arguments[10];

			int minimumCount = 1000;
			DateOnly fromDate = new DateOnly(2009, 1, 1);
			DateOnly toDate = new DateOnly(2011, 1, 1);
			string logDirectory = null;

			var analyzer = new CorrelationAnalyzer(
				earningsPath,
				priceDataDirectory,
				features,
				minimumCount,
				fromDate,
				toDate,
				forecastDays,
				nominalCorrelationOutput,
				relativeCorrelationOutput,
				presenceOutput,
				appearanceOutput,
				disappearanceOutput,
				featureCountOutput,
				weekdayOutput
			);
			analyzer.Run();
		}
	}
}