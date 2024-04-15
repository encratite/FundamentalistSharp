namespace Fundamentalist.Trainer
{
	internal class TrainerOptions
	{
		public int Features { get; set; }
		public int ForecastDays { get; set; }
		public DateTime TrainingDate { get; set; }
		public DateTime TestDate { get; set; }

		public void Print()
		{
			Console.WriteLine($"  Features: {Features}");
			Console.WriteLine($"  ForecastDays: {ForecastDays}");
			Console.WriteLine($"  TrainingDate: {TrainingDate.ToShortDateString()}");
			Console.WriteLine($"  TestDate: {TestDate.ToShortDateString()}");
		}
	}
}
