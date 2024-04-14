namespace Fundamentalist.Xblr.Json
{
	internal class Fact
	{
		public string Label { get; set; }
		public string Description { get; set; }
		public Dictionary<string, FactValues[]> Units { get; set; }
	}
}
