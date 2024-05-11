namespace Fundamentalist.Trainer
{
	internal class TrainerOptions
	{
		public int DaysSinceEarnings { get; set; }
		public int ForecastDays { get; set; }

		public DateOnly TrainingDate { get; set; }
		public DateOnly TestDate { get; set; }

		public decimal OutperformLimit { get; set; }
		public decimal UnderperformLimit { get; set; }

		public int CommonFeatures { get; set; }

		public string NominalCorrelationPath { get; set; }
		public decimal NominalCorrelationLimit { get; set; }

		public string PresencePath { get; set; }
		public decimal PresenceLimit { get; set; }

		public decimal MinimumPrice { get; set; }

		public void Print()
		{
			Console.WriteLine($"  DaysSinceEarnings: {DaysSinceEarnings}");
			Console.WriteLine($"  ForecastDays: {ForecastDays}");

			Console.WriteLine($"  TrainingDate: {TrainingDate}");
			Console.WriteLine($"  TestDate: {TestDate}");

			Console.WriteLine($"  OutperformLimit: {OutperformLimit}");
			Console.WriteLine($"  UnderperformLimit: {UnderperformLimit}");

			Console.WriteLine($"  CommonFeatures: {CommonFeatures}");

			Console.WriteLine($"  NominalCorrelationPath: {NominalCorrelationPath}");
			Console.WriteLine($"  NominalCorrelationLimit: {NominalCorrelationLimit}");

			Console.WriteLine($"  PresencePath: {PresencePath}");
			Console.WriteLine($"  PresenceLimit: {PresenceLimit}");

			Console.WriteLine($"  MinimumPrice: {MinimumPrice}");
		}
	}
}
