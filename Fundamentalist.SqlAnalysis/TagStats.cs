namespace Fundamentalist.Analysis
{
	internal class TagStats
	{
		public string Name { get; set; }
		public List<Observation> Observations { get; set; }  = new List<Observation>();
		public decimal? SpearmanCoefficient { get; set; }
		public decimal? Covariance { get; set; }

		public TagStats(string name)
		{
			Name = name;
		}

		public override string ToString()
		{
			return $"{Name} ({Observations.Count} samples)";
		}
	}
}
