namespace Fundamentalist.Trainer
{
	internal class TrainerOptions
	{
		public int ForecastDays { get; set; }
		public DateTime TrainingDate { get; set; }
		public DateTime TestDate { get; set; }
		public decimal OutperformLimit { get; set; }
		public decimal UnderperformLimit { get; set; }

		public void Print()
		{
			Console.WriteLine($"  ForecastDays: {ForecastDays}");
			Console.WriteLine($"  TrainingDate: {TrainingDate.ToShortDateString()}");
			Console.WriteLine($"  TestDate: {TestDate.ToShortDateString()}");
			Console.WriteLine($"  OutperformLimit: {OutperformLimit}");
			Console.WriteLine($"  UnderperformLimit: {UnderperformLimit}");
		}
	}
}
