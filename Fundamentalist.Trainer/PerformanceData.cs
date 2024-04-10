namespace Fundamentalist.Trainer
{
	internal class PerformanceData
	{
		public string Description { get; set; }
		public decimal Performance { get; set; }

		public PerformanceData(string description, decimal performance)
		{
			Description = description;
			Performance = performance;
		}
	}
}
