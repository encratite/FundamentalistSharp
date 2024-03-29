using System.Reflection;

namespace Fundamentalist.Trainer
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
				Console.WriteLine($"{name.Name} <scraper data directory>");
			}
			string path = Path.Combine(arguments[0], "..");
			Directory.SetCurrentDirectory(path);
			var trainer = new Trainer();
			// trainer.Run();
			trainer.AnalyzeData();
		}
	}
}