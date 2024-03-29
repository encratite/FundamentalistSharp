namespace Fundamentalist.Common
{
	public class FeatureName
	{
		public string Name { get; set; }
		public bool HasValue { get; set; }

		public FeatureName(string container, string name, object value)
		{
			Name = $"{container}.{name}";
			HasValue = value != null;
		}
	}
}
