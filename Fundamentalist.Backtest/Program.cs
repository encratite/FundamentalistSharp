using Fundamentalist.Backtest.Strategies;
using System.Reflection;
using System.Text.Json;

namespace Fundamentalist.Backtest
{
	internal class Program
	{
		static void Main(string[] arguments)
		{
			if (arguments.Length != 1)
			{
				var assembly = Assembly.GetExecutingAssembly();
				var name = assembly.GetName();
				Console.WriteLine("Usage:");
				Console.WriteLine($"{name.Name} <configuration JSON file>");
				return;
			}
			string jsonConfiguration = File.ReadAllText(arguments[0]);
			var options = new JsonSerializerOptions
			{
				PropertyNameCaseInsensitive = true
			};
			var configuration = JsonSerializer.Deserialize<Configuration>(jsonConfiguration, options);
			configuration.Validate();
			var strategyConfiguration = new ClenowMomentumConfiguration();
			var strategy = new ClenowMomentumStrategy(strategyConfiguration);
			var backtest = new Backtest();
			var performance = backtest.Run(strategy, configuration);
			backtest.SavePerformance(performance);
		}
	}
}
