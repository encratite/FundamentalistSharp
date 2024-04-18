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
			/*
			for (int features = 100; features <= 500; features += 100)
			{
				var options = new TrainerOptions
				{
					LoaderFeatures = 500,
					Features = features,
					ForecastDays = 5,
					TrainingDate = new DateTime(2020, 1, 1),
					TestDate = new DateTime(2023, 1, 1),
					MinimumGain = 0.075m
				};
				trainer.Run(options, earningsPath, priceDataDirectory);
			}
			*/
			var options = new TrainerOptions
			{
				Features = 0,
				ForecastDays = 1,
				TrainingDate = new DateTime(2010, 1, 1),
				TestDate = new DateTime(2022, 1, 1),
				MinimumGain = 0.03m
			};
			trainer.Run(options, earningsPath, priceDataDirectory);
		}
	}
}