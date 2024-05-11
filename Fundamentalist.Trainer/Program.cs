using System.Reflection;

namespace Fundamentalist.Trainer
{
	internal static class Program
	{
		private static void Main(string[] arguments)
		{
			if (arguments.Length != 4)
			{
				var assembly = Assembly.GetExecutingAssembly();
				var name = assembly.GetName();
				Console.WriteLine("Usage:");
				Console.WriteLine($"{name.Name} <path to earnings .csv file> <price data directory> <nominal correlation path> <presence path>");
				return;
			}
			string earningsPath = arguments[0];
			string priceDataDirectory = arguments[1];
			string nominalCorrelationPath = arguments[2];
			string presencePath = arguments[3];
			Run(earningsPath, priceDataDirectory, nominalCorrelationPath, presencePath);
			Console.Beep();
		}

		private static void Run(string earningsPath, string priceDataDirectory, string nominalCorrelationPath, string presencePath)
		{
			var trainer = new Trainer();
			var commonFeatures = new int[]
			{
				/*
				100,
				150,
				200,
				500,
				1000,
				2000,
				3000
				*/
				1000
			};
			foreach (int features in commonFeatures)
			{
				var options = new TrainerOptions
				{
					DaysSinceEarnings = 6,
					ForecastDays = 60,
					TrainingDate = new DateOnly(2018, 1, 1),
					TestDate = new DateOnly(2023, 1, 1),
					OutperformLimit = 0.06m,
					UnderperformLimit = -0.06m,
					CommonFeatures = features,
					NominalCorrelationPath = nominalCorrelationPath,
					NominalCorrelationLimit = 0.03m,
					PresencePath = presencePath,
					PresenceLimit = 0.02m,
					MinimumPrice = 1.0m
				};
				trainer.Run(options, earningsPath, priceDataDirectory);
			}
		}
	}
}