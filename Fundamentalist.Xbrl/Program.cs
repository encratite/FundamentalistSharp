using System.Reflection;

namespace Fundamentalist.Xblr
{
	internal static class Program
	{
		private static void Main(string[] arguments)
		{
			if (arguments.Length != 1)
			{
				var assembly = Assembly.GetExecutingAssembly();
				var name = assembly.GetName();
				Console.WriteLine("Usage:");
				Console.WriteLine($"{name.Name} <path to XBLR JSON files>");
				return;
			}
			string path = arguments[0];
			var parser = new XbrlParser();
			parser.Run(path);
		}
	}
}