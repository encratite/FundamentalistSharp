using MongoDB.Bson;

namespace Fundamentalist.Common.Document
{
	public class StrategyPerformance
	{
		public ObjectId Id { get; set; }
		public DateTimeOffset Time { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public List<EquitySample> EquityCurve { get; set; } = new List<EquitySample>();
		public List<StrategyAction> Actions { get; set; } = new List<StrategyAction>();
	}
}
