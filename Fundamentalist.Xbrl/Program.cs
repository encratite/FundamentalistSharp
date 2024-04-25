using Fundamentalist.Common;
using System.Reflection;

namespace Fundamentalist.Xblr
{
	internal static class Program
	{
		private static void Main(string[] arguments)
		{
			if (arguments.Length != 6)
			{
				var assembly = Assembly.GetExecutingAssembly();
				var name = assembly.GetName();
				Console.WriteLine("Usage:");
				Console.WriteLine($"{name.Name} <path to directory containing XBLR JSON files> <path to company tickers> <feature frequency output path> <training data output path>");
				return;
			}
			string xbrlDirectory = arguments[0];
			string priceDataDirectory = arguments[1];
			string tickerPath = arguments[2];
			string frequencyPath = arguments[3];
			string csvOutputPath = arguments[4];
			int featureCount = int.Parse(arguments[5]);
			var parser = new XbrlParser();
			parser.Load(xbrlDirectory, priceDataDirectory, tickerPath, featureCount);
			parser.WriteFrequency(frequencyPath);
			parser.WriteCsv(csvOutputPath);
		}
	}
}