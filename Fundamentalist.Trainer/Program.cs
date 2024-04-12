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
				return;
			}
			string path = Path.Combine(arguments[0], "..");
			Directory.SetCurrentDirectory(path);
			RunTests();
		}

		private static void RunTests()
		{
			var trainer = new Trainer();
			for (int trainingYear = 2019; trainingYear <= 2022; trainingYear++)
			{
				trainer.Run(new TrainerOptions
				{
					ForecastDays = 5,
					TrainingDate = new DateTime(trainingYear, 1, 1),
					TestDate = new DateTime(2023, 1, 1)
				});
			}
		}
	}
}