using System.Diagnostics;

namespace Fundamentalist.Common
{
	public class PerformanceTimer : IDisposable
	{
		private string _description;
		private Stopwatch _stopwatch;

		public PerformanceTimer(string description)
		{
			_description = description;
			_stopwatch = new Stopwatch();
			_stopwatch.Start();
			Console.WriteLine($"Performing step \"{_description}\"");
		}

		public void Dispose()
		{
			Console.WriteLine($"Finished step \"{_description}\" in {_stopwatch.Elapsed.TotalSeconds:F1} s");
		}
	}
}
