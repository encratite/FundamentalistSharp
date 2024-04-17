using Fundamentalist.Common;
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
			for (int trainingYear = 2020; trainingYear <= 2022; trainingYear++)
			{
				var options = new TrainerOptions
				{
					Features = 500,
					ForecastDays = 5,
					TrainingDate = new DateTime(trainingYear, 1, 1),
					TestDate = new DateTime(2023, 1, 1)
				};
				trainer.Run(options, earningsPath, priceDataDirectory);
			}
		}
	}
}