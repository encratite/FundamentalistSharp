namespace Fundamentalist.Trainer
{
	internal class TrainerOptions
	{
		public int ForecastDays { get; set; }
		public DateTime TrainingDate { get; set; }
		public DateTime TestDate { get; set; }

		public void Print()
		{
			Console.WriteLine($"  ForecastDays: {ForecastDays}");
			Console.WriteLine($"  TrainingDate: {TrainingDate.ToShortDateString()}");
			Console.WriteLine($"  TestDate: {TestDate.ToShortDateString()}");
		}
	}
}
