namespace Fundamentalist.Correlation
{
	internal class IndexFloat
	{
		public float Value { get; set; }
		public int Index { get; set; }

		public IndexFloat(float value, int index)
		{
			Value = value;
			Index = index;
		}
	}
}
