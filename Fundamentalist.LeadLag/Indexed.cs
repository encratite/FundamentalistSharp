namespace Fundamentalist.LeadLag
{
	internal struct Indexed
	{
		public decimal Value;
		public int Index;

		public Indexed(decimal value, int index)
		{
			Value = value;
			Index = index;
		}
	}
}
