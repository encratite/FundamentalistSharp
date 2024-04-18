using System.Security.Cryptography.X509Certificates;

namespace Fundamentalist.Trainer
{
	internal class TrainerOptions
	{
		public int? LoaderFeatures { get; set; }
		public int Features { get; set; }
		public int ForecastDays { get; set; }
		public DateTime TrainingDate { get; set; }
		public DateTime TestDate { get; set; }
		public decimal MinimumGain { get; set; }

		public void Print()
		{
			Console.WriteLine($"  Features: {Features}");
			Console.WriteLine($"  ForecastDays: {ForecastDays}");
			Console.WriteLine($"  TrainingDate: {TrainingDate.ToShortDateString()}");
			Console.WriteLine($"  TestDate: {TestDate.ToShortDateString()}");
			Console.WriteLine($"  MinimumGain: {MinimumGain}");
		}
	}
}
