namespace Fundamentalist.SqlAnalysis
{
	internal struct IndexValue
	{
		public decimal Value;
		public int Index;

		public IndexValue(decimal value, int index)
		{
			Value = value;
			Index = index;
		}
	}
}
