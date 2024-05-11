namespace Fundamentalist.CsvGenerator
{
	internal class IndustrySector
	{
		public string Industry { get; set; }
		public string Sector { get; set; }

		public IndustrySector(string industry, string sector)
		{
			Industry = industry;
			Sector = sector;
		}
	}
}
