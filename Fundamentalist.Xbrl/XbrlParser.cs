using Fundamentalist.Xblr.Json;
using NetJSON;
using System.Text.Json;

namespace Fundamentalist.Xblr
{
	internal class XbrlParser
	{
		private Dictionary<string, int> _factFrequenies;
		private int _factCount;
		private int _progress;
		private int _total;

		public void Run(string directory)
		{
			var paths = Directory.GetFiles(directory, "*.json");
			_factFrequenies = new Dictionary<string, int>();
			_factCount = 0;
			_progress = 0;
			_total = paths.Length;
			// Parallel.ForEach(paths, ProcessFile);
			foreach (string path in paths)
				ProcessFile(path);
			Console.WriteLine($"Processed all files and encountered {_factFrequenies.Count} facts in total");
			foreach (var pair in _factFrequenies)
			{
				Console.WriteLine($"{pair.Key}: {(decimal)pair.Value / _factCount:P1}");
			}
		}

		private void ProcessFile(string path)
		{
			string json = File.ReadAllText(path);
			/*
			var settings = new NetJSONSettings
			{
				CaseSensitive = false,
				DateStringFormat = "yyyy-MM-dd"
			};
			var companyFacts = NetJSON.NetJSON.Deserialize<CompanyFacts>(json, settings);
			*/
			var options = new JsonSerializerOptions
			{
				PropertyNameCaseInsensitive = true
			};
			var companyFacts = JsonSerializer.Deserialize<CompanyFacts>(json, options);
			if (companyFacts.Facts == null)
				return;
			foreach (var dictionary in companyFacts.Facts.Values)
			{
				foreach (var pair in dictionary)
				{
					string factName = pair.Key;
					var fact = pair.Value;
					int count = fact.Units.Values.First().Length;
					lock (_factFrequenies)
					{
						if (_factFrequenies.ContainsKey(factName))
							_factFrequenies[factName] += count;
						else
							_factFrequenies[factName] = count;
						_factCount += count;
					}
				}
			}
			Interlocked.Increment(ref _progress);
			if (_progress > 0 && _progress % 100 == 0 || _progress == _total)
				Console.WriteLine($"Processed {_progress} out of {_total} files ({(decimal)_progress / _total:P1})");
		}
	}
}
