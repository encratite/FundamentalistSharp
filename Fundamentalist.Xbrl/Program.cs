using System.Reflection;

namespace Fundamentalist.Xblr
{
	internal static class Program
	{
		private static void Main(string[] arguments)
		{
			if (arguments.Length != 3)
			{
				var assembly = Assembly.GetExecutingAssembly();
				var name = assembly.GetName();
				Console.WriteLine("Usage:");
				Console.WriteLine($"{name.Name} <path to directory containing XBLR JSON files> <path to company tickers> <feature frequency output path> <training data output path>");
				return;
			}
			string xbrlDirectory = arguments[0];
			string tickerPath = arguments[1];
			string frequencyPath = arguments[2];
			var parser = new XbrlParser();
			parser.Run(xbrlDirectory, tickerPath, frequencyPath);
		}
	}
}