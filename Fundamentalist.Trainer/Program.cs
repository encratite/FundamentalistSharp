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
			for (float minimumScore = 0; minimumScore < 0.05f; minimumScore += 0.01f)
			{
				var options = new TrainerOptions
				{
					Features = 1000,
					ForecastDays = 5,
					TrainingDate = new DateTime(2020, 1, 1),
					TestDate = new DateTime(2023, 1, 1),
					FeatureSelection = new HashSet<int>
					{
						// Positive:
						674,
						923,
						530,
						277,
						238,

						689,
						461,
						476,
						690,
						571,

						635,
						891,
						795,
						837,
						832,

						// Negative:
						398,
						420,
						937,
						433,
						873,

						882,
						667,
						821,
						810,
						246,

						579,
						763,
						460,
						519,
						842,
					},
					MinimumSignals = 2,
					MinimumScore = minimumScore
				};
				trainer.Run(options, earningsPath, priceDataDirectory);
			}
		}
	}
}