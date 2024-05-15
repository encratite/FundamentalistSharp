namespace Fundamentalist.SqlAnalysis
{
	internal class Observation
	{
		public decimal X { get; set; }
		public decimal Y { get; set; }

		public Observation(decimal x, decimal y)
		{
			X = x;
			Y = y;
		}
	}
}
