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
			RunTests();
		}

		private static void RunTests()
		{
			var trainer = new Trainer();
			for (int days = 15; days <= 120; days *= 2)
			{
				trainer.Run(new TrainerOptions
				{
					FinancialStatementCount = 4,
					LookaheadDays = days,
					HistoryDays = 0,
					MinPerformance = 0.05m
				});
			}
		}
	}
}