namespace Fundamentalist.SqlAnalysis
{
	internal class FactStats
	{
		public string Name { get; set; }
		public List<Observation> Observations { get; set; }  = new List<Observation>();
		public decimal? SpearmanCoefficient { get; set; }

		public FactStats(string name)
		{
			Name = name;
		}

		public override string ToString()
		{
			return $"{Name} ({Observations.Count} samples)";
		}
	}
}
