using System.Reflection;

namespace Fundamentalist.CsvGenerator
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
				Console.WriteLine($"{name.Name} <path to EDGAR zip files> <CSV output directory>");
				return;
			}
			int offset = 0;
			Func<string> getArgument = () => arguments[offset++];
			string edgarPath = getArgument();
			string csvOutputDirectory = getArgument();
			var generator = new CsvGenerator();
			generator.WriteCsvFiles(edgarPath, csvOutputDirectory);
		}
	}
}
