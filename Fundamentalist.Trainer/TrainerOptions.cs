namespace Fundamentalist.Trainer
{
	internal class TrainerOptions
	{
		public int HistoryDays { get; set; }
		public int ForecastDays { get; set; }
		public DateTime SplitDate { get; set; }

		public void Print()
		{
			Console.WriteLine($"  HistoryDays: {HistoryDays}");
			Console.WriteLine($"  ForecastDays: {ForecastDays}");
			Console.WriteLine($"  SplitDate: {SplitDate}");
		}
	}
}
