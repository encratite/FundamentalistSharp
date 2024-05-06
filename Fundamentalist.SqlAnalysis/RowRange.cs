namespace Fundamentalist.SqlAnalysis
{
	internal class RowRange
	{
		public PriceKey Key { get; set; }
		public int Start { get; set; }
		public int End { get; set; }

		public RowRange(PriceKey key, int start, int end)
		{
			Key = key;
			Start = start;
			End = end;
		}

		public override string ToString()
		{
			return $"{Key}: {Start} - {End}";
		}
	}
}
