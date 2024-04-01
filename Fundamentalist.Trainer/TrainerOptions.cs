namespace Fundamentalist.Trainer
{
	internal class TrainerOptions
	{
		public int FinancialStatementCount { get; set; }
		public int LookaheadDays { get; set; }
		public int HistoryDays { get; set; }
		public decimal MinPerformance { get; set; }
		public string DataPointsPath { get; set; }

		public void Print()
		{
			// Console.WriteLine("Options used:");
			Console.WriteLine($"  FinancialStatementCount: {FinancialStatementCount}");
			Console.WriteLine($"  LookaheadDays: {LookaheadDays}");
			Console.WriteLine($"  HistoryDays: {HistoryDays}");
			Console.WriteLine($"  MinPerformance: {MinPerformance}");
		}
	}
}
