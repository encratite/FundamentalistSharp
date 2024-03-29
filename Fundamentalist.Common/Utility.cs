using System.Text.Json;

namespace Fundamentalist.Common
{
	public static class Utility
	{
		public static T Deserialize<T>(string json)
		{
			var options = new JsonSerializerOptions
			{
				PropertyNameCaseInsensitive = true
			};
			var output = JsonSerializer.Deserialize<T>(json, options);
			return output;
		}

		public static void WriteError(string message)
		{
			var previousColor = Console.ForegroundColor;
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine(message);
			Console.ForegroundColor = previousColor;
		}
	}
}
