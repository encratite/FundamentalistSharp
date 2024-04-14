using System.Reflection;

namespace Fundamentalist.Xblr
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
				Console.WriteLine($"{name.Name} <path to directory containing XBLR JSON files> <feature frequency output path>");
				return;
			}
			string xbrlDirectory = arguments[0];
			string frequencyPath = arguments[1];
			var parser = new XbrlParser();
			parser.Run(xbrlDirectory, frequencyPath);
		}
	}
}