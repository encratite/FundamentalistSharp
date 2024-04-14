using Fundamentalist.Xblr.Json;
using Fundamentalist.Xbrl.Json;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;
using System.Text.Json;

namespace Fundamentalist.Xblr
{
	internal class XbrlParser
	{
		private Dictionary<string, int> _factFrequenies;
		private ConcurrentBag<CompanyEarnings> _companyEarnings;
		private int _progress;
		private int _total;
		private Stopwatch _stopwatch;
		private Dictionary<int, string> _tickers;
		private int _tickerErrors;

		public void Run(string directory, string tickerPath, string frequencyPath)
		{
			_stopwatch = new Stopwatch();
			_stopwatch.Start();
			ReadTickers(tickerPath);
			var paths = Directory.GetFiles(directory, "*.json");
			_factFrequenies = new Dictionary<string, int>();
			_companyEarnings = new ConcurrentBag<CompanyEarnings>();
			_progress = 0;
			_total = paths.Length;
			_tickerErrors = 0;
			var queue = new ConcurrentQueue<string>(paths);
			var threads = new List<Thread>();
			for (int i = 0; i < Environment.ProcessorCount; i++)
			{
				var thread = new Thread(() => RunThread(queue));
				thread.Start();
				threads.Add(thread);
			}
			foreach (var thread in threads)
				thread.Join();
			var frequencies = _factFrequenies.AsEnumerable().OrderByDescending(x => x.Value);
			using (var writer = new StreamWriter(frequencyPath, false))
			{
				foreach (var x in frequencies)
					writer.WriteLine($"{x.Key}: {x.Value}");
			}
			_stopwatch.Stop();
			Console.WriteLine($"Processed all files in {_stopwatch.Elapsed.TotalSeconds:F1} s and encountered {_factFrequenies.Count} facts in total");
			Console.WriteLine($"Discarded {_tickerErrors} companies due to missing ticker data");
		}

		private void ReadTickers(string tickerPath)
		{
			_tickers = new Dictionary<int, string>();
			string json = File.ReadAllText(tickerPath);
			var ticker = JsonSerializer.Deserialize<Dictionary<string, Ticker>>(json);
			foreach (var x in ticker.Values)
				_tickers[x.Cik] = x.Symbol;
		}

		private void RunThread(ConcurrentQueue<string> queue)
		{
			var factFrequencies = new Dictionary<string, int>();
			string path;
			while (queue.TryDequeue(out path))
				ProcessFile(path, factFrequencies);
			lock (_factFrequenies)
			{
				foreach (var x in factFrequencies)
				{
					if (_factFrequenies.ContainsKey(x.Key))
						_factFrequenies[x.Key] += x.Value;
					else
						_factFrequenies[x.Key] = x.Value;
				}
			}
		}

		private void ProcessFile(string path, Dictionary<string, int> factFrequencies)
		{
			string json = File.ReadAllText(path);
			var options = new JsonSerializerOptions
			{
				PropertyNameCaseInsensitive = true
			};
			var companyFacts = JsonSerializer.Deserialize<CompanyFacts>(json, options);
			if (companyFacts.Facts == null)
				return;
			string ticker;
			if (_tickers.TryGetValue(companyFacts.Cik, out ticker))
			{
				// Console.WriteLine($"Unable to determine ticker of CIK {companyFacts.Cik}");
				Interlocked.Increment(ref _tickerErrors);
				return;
			}
			var earnings = new CompanyEarnings(ticker);
			foreach (var dictionary in companyFacts.Facts.Values)
			{
				foreach (var pair in dictionary)
				{
					string factName = pair.Key;
					var fact = pair.Value;
					var factValues = fact.Units.Values.First();
					int count = factValues.Length;
					if (factFrequencies.ContainsKey(factName))
						factFrequencies[factName] += count;
					else
						factFrequencies[factName] = count;
					foreach (var factValue in factValues)
					{
						var facts = earnings.Facts;
						var key = factValue.Filed;
						if (!facts.ContainsKey(key))
							facts[key] = new Dictionary<string, FactValues>();
						earnings.Facts[factValue.Filed][factName] = factValue;
					}
				}
			}
			_companyEarnings.Add(earnings);
			Interlocked.Increment(ref _progress);
			if (_progress > 0 && _progress % 100 == 0 || _progress == _total)
				Console.WriteLine($"Processed {_progress} out of {_total} files ({(decimal)_progress / _total:P1}, {_progress / _stopwatch.Elapsed.TotalSeconds:F1}/s)");
		}
	}
}
