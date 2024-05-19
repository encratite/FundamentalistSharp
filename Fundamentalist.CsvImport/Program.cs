using System.Reflection;

namespace Fundamentalist.CsvGenerator
{
	internal class Program
	{
		static void Main(string[] arguments)
		{
			if (arguments.Length != 3)
			{
				var assembly = Assembly.GetExecutingAssembly();
				var name = assembly.GetName();
				Console.WriteLine("Usage:");
				Console.WriteLine($"{name.Name} <path to EDGAR zip files> <Sharadar price .csv> <MongoDB connection string>");
				return;
			}
			int offset = 0;
			Func<string> getArgument = () => arguments[offset++];
			string edgarPath = getArgument();
			string priceCsvPath = getArgument();
			string connectionString = getArgument();
			var import = new CsvImport();
			import.ImportCsvFiles(edgarPath, priceCsvPath, connectionString);
		}
	}
}
