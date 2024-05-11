using System.Reflection;

namespace Fundamentalist.LeadLag
{
	internal class Program
	{
		static void Main(string[] arguments)
		{
			if (arguments.Length != 2)
			{
				var assembly = Assembly.GetExecutingAssembly();
				var name = assembly.GetName();
				Console.WriteLine("Usage:");
				Console.WriteLine($"{name.Name} <price data directory to download files to> <output path>");
				return;
			}
			string priceDataDirectory = arguments[0];
			string outputPath = arguments[1];
			var detector = new LeadLagDetector(new DateOnly(2020, 1, 1), new DateOnly(2023, 1, 1), 1);
			detector.Run(priceDataDirectory, outputPath);
		}
	}
}
