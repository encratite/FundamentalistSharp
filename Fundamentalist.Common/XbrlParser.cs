using Fundamentalist.Common.Json;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;

namespace Fundamentalist.Common
{
	public class XbrlParser
	{
		private const bool XbrlTestMode = false;
		private const bool TickerTestMode = false;

		public IEnumerable<Ticker> Tickers
		{
			get => _tickers;
		}

		public IEnumerable<CompanyEarnings> Earnings
		{
			get => _companyEarnings;
		}

		private Dictionary<string, int> _factFrequenies;
		private ConcurrentBag<CompanyEarnings> _companyEarnings;
		private int _progress;
		private int _total;
		private Stopwatch _stopwatch;
		private List<Ticker> _tickers;
		private int _tickerErrors;
		private int? _maximumFeatureCount;
		private ConcurrentDictionary<string, int> _formFrequency;

		public void Load(string xbrlDirectory, string tickerPath, string priceDataDirectory, int? maximumFeatureCount = null)
		{
			_maximumFeatureCount = maximumFeatureCount;
			_stopwatch = new Stopwatch();
			_stopwatch.Start();
			Console.WriteLine($"Reading tickers from {tickerPath}");
			ReadTickers(tickerPath);
			Console.WriteLine($"Processing XBRL files from {xbrlDirectory}");
			var paths = Directory.GetFiles(xbrlDirectory, "*.json");
			if (XbrlTestMode)
				paths = paths.Take(10).ToArray();
			_factFrequenies = new Dictionary<string, int>();
			_companyEarnings = new ConcurrentBag<CompanyEarnings>();
			_formFrequency = new ConcurrentDictionary<string, int>();
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
			AnalyzeFormFrequency();
			_stopwatch.Stop();
			Console.WriteLine($"Processed all files in {_stopwatch.Elapsed.TotalSeconds:F1} s and encountered {_factFrequenies.Count} facts in total");
			Console.WriteLine($"Discarded {_tickerErrors} companies due to missing ticker data");
		}

		public void WriteCsv(string outputPath)
		{
			var featureIndices = new Dictionary<string, int>();
			int index = 0;
			IEnumerable<KeyValuePair<string, int>> frequencies = _factFrequenies.AsEnumerable().OrderByDescending(x => x.Value);
			if (_maximumFeatureCount.HasValue)
				frequencies = frequencies.Take(_maximumFeatureCount.Value);
			var selectedFacts = frequencies.Select(x => x.Key).ToList();
			foreach (string name in selectedFacts)
			{
				featureIndices[name] = index;
				index++;
			}
			Console.WriteLine($"Writing CSV output to {outputPath}");
			using (var writer = new StreamWriter(outputPath, false))
			{
				var writeTokens = (List<string> tokens) =>
				{
					string line = string.Join(',', tokens);
					writer.WriteLine(line);
				};
				var headerTokens = new List<string>
				{
					"Ticker",
					"Date",
				};
				headerTokens.AddRange(selectedFacts);
				writeTokens(headerTokens);
				foreach (var earnings in _companyEarnings)
				{
					foreach (var x in earnings.Facts)
					{
						var date = x.Key;
						var facts = x.Value;
						var tokens = new List<string>
						{
							earnings.Ticker.Symbol,
							date.ToShortDateString(),
						};
						var features = new decimal[selectedFacts.Count];
						foreach (var fact in facts)
						{
							string name = fact.Key;
							var factValue = fact.Value;
							int factIndex;
							if (!featureIndices.TryGetValue(name, out factIndex))
								continue;
							features[factIndex] = factValue.Value;
						}
						foreach (var feature in features)
							tokens.Add(feature.ToString());
						writeTokens(tokens);
					}
				}
			}
		}

		public void WriteFrequency(string frequencyPath)
		{
			var frequencies = _factFrequenies.AsEnumerable().OrderByDescending(x => x.Value);
			using (var writer = new StreamWriter(frequencyPath, false))
			{
				foreach (var x in frequencies)
					writer.WriteLine($"{x.Key}: {x.Value}");
			}
		}

		private void ReadTickers(string tickerPath)
		{
			_tickers = DataReader.GetTickersFromJson(tickerPath);
			if (TickerTestMode)
				_tickers = _tickers.Take(50).ToList();
		}

		private void RunThread(ConcurrentQueue<string> queue)
		{
			var factFrequencies = new Dictionary<string, int>();
			string path;
			while (queue.TryDequeue(out path))
			{
				ProcessFile(path, factFrequencies);
				Interlocked.Increment(ref _progress);
				if (_progress > 0 && _progress % 100 == 0 || _progress == _total)
					Console.WriteLine($"Processed {_progress} out of {_total} files ({(decimal)_progress / _total:P1}, {_progress / _stopwatch.Elapsed.TotalSeconds:F1}/s)");
			}
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
			var ticker = _tickers.LastOrDefault(x => x.Cik == companyFacts.Cik);
			if (ticker == null)
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
						_formFrequency.AddOrUpdate(factValue.Form, 1, (x, y) => y + 1);
						var facts = earnings.Facts;
						var key = factValue.Filed;
						if (!facts.ContainsKey(key))
							facts[key] = new Dictionary<string, FactValues>();
						facts[factValue.Filed][factName] = factValue;
					}
				}
			}
			if (earnings.Facts.Count > 0)
				_companyEarnings.Add(earnings);
		}

		private void AnalyzeFormFrequency()
		{
			Console.WriteLine("Form statistics:");
			int sum = _formFrequency.Values.Sum();
			foreach (var pair in _formFrequency.OrderBy(x => x.Key))
				Console.WriteLine($"{pair.Key}: {(decimal)pair.Value / sum:P2}");
		}
	}
}
