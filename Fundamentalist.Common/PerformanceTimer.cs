using System.Diagnostics;

namespace Fundamentalist.Common
{
	public class PerformanceTimer : IDisposable
	{
		private string _endMessage;
		private Stopwatch _stopwatch;

		public PerformanceTimer(string startMessage, string endMessage)
		{
			_endMessage = endMessage;
			_stopwatch = new Stopwatch();
			_stopwatch.Start();
			Console.WriteLine(startMessage);
		}

		public void Dispose()
		{
			Console.WriteLine($"{_endMessage} in {_stopwatch.Elapsed.TotalSeconds:F1} s");
		}
	}
}
