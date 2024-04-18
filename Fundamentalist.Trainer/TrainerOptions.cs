using System.Security.Cryptography.X509Certificates;

namespace Fundamentalist.Trainer
{
	internal class TrainerOptions
	{
		public int Features { get; set; }
		public int ForecastDays { get; set; }
		public DateTime TrainingDate { get; set; }
		public DateTime TestDate { get; set; }
		public HashSet<int> FeatureSelection { get; set; }
		public int MinimumSignals { get; set; }
		public float MinimumScore { get; set; }

		public void Print()
		{
			Console.WriteLine($"  Features: {Features}");
			Console.WriteLine($"  ForecastDays: {ForecastDays}");
			Console.WriteLine($"  TrainingDate: {TrainingDate.ToShortDateString()}");
			Console.WriteLine($"  TestDate: {TestDate.ToShortDateString()}");
			if (FeatureSelection != null)
				Console.WriteLine($"  FeatureSelection: {FeatureSelection.Count} features");
			Console.WriteLine($"  MinimumSignals: {MinimumSignals}");
			Console.WriteLine($"  MinimumScore: {MinimumScore}");
		}
	}
}
