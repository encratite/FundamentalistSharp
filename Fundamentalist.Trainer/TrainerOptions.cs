namespace Fundamentalist.Trainer
{
	internal class TrainerOptions
	{
		public int HistoryDays { get; set; }
		public DateTime SplitDate { get; set; }

		public void Print()
		{
			Console.WriteLine($"  HistoryDays: {HistoryDays}");
		}
	}
}
