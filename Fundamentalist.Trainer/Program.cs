using System.Reflection;

namespace Fundamentalist.Trainer
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
				Console.WriteLine($"{name.Name} <path to earnings .csv file> <price data directory>");
				return;
			}
			string earningsPath = arguments[0];
			string priceDataDirectory = arguments[1];
			Run(earningsPath, priceDataDirectory);
		}

		private static void Run(string earningsPath, string priceDataDirectory)
		{
			var trainer = new Trainer();
			var options = new TrainerOptions
			{
				Features = 0,
				ForecastDays = 1,
				TrainingDate = new DateTime(2018, 1, 1),
				TestDate = new DateTime(2022, 1, 1),
				MinimumGain = 0.01m
			};
			trainer.Run(options, earningsPath, priceDataDirectory);
		}
	}
}