using System.Collections.Concurrent;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Text.Json;

namespace Fundamentalist.SqlAnalysis
{
	internal class SqlAnalysis
	{
		private Configuration _configuration;

		public void Run(Configuration configuration)
		{
			_configuration = configuration;
			Console.WriteLine("Loading SEC filings and price data from database");
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			DataTable factsTable;
			using (var connection = new SqlConnection(_configuration.ConnectionString))
			{
				connection.Open();
				factsTable = GetFactsTable(connection);
			}
			stopwatch.Stop();
			Console.WriteLine($"Finished loading data in {stopwatch.Elapsed.TotalSeconds:F1} s");
			EvaluateFacts(factsTable, performanceData);
		}

		private DataTable GetFactsTable(SqlConnection connection)
		{
			string query = @"
				select
					symbol,
					filed,
					name,
					unit
				from
					fact join ticker
					on fact.cik = ticker.cik
				where
					fact.filed >= @from
					and fact.filed < @to
					and fact.form = @form
					and ticker.exclude = 0";
			var dataTable = new DataTable();
			using (var command = new SqlCommand(query, connection))
			{
				var parameters = command.Parameters;
				parameters.AddWithValue("@from", _configuration.From.Value);
				parameters.AddWithValue("@to", _configuration.To.Value);
				parameters.AddWithValue("@form", _configuration.Form);
				using (var adapter = new SqlDataAdapter(command))
				{
					adapter.Fill(dataTable);
				}
			}
			return dataTable;
		}

		private void GetDateOpenClose(DataRow row, out DateTime date, out decimal open, out decimal close)
		{
			date = row.Field<DateTime>("date");
			open = row.Field<decimal>("open_price");
			close = row.Field<decimal>("close_price");
		}

		private decimal GetPerformance(decimal stockFrom, decimal stockTo, decimal indexFrom, decimal indexTo)
		{
			return stockTo / stockFrom - indexTo / indexFrom;
		}

		private void EvaluateFacts(DataTable factsTable, Dictionary<PriceKey, decimal> performanceData)
		{
			Console.WriteLine("Evaluating facts");
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			var facts = new ConcurrentDictionary<string, StatsAggregator>();
			var units = new ConcurrentDictionary<string, string>();
			Parallel.ForEach(factsTable.AsEnumerable(), row =>
			{
				string symbol = row.Field<string>("symbol");
				DateTime filed = row.Field<DateTime>("filed");
				string fact = row.Field<string>("name");
				string unit = row.Field<string>("unit");
				if (!units.ContainsKey(fact))
					units[fact] = unit;
				var priceKey = new PriceKey(symbol, filed);
				decimal performance;
				if (!performanceData.TryGetValue(priceKey, out performance))
					return;
				facts.AddOrUpdate(fact, new StatsAggregator(performance), (fact, aggregator) =>
				{
					aggregator.Add(performance);
					return aggregator;
				});
			});
			var commonFacts = facts.Where(pair => (decimal)pair.Value.Count / performanceData.Count > _configuration.MinimumFrequency.Value).ToList();
			Parallel.ForEach(commonFacts, pair => pair.Value.UpdateStats());
			stopwatch.Stop();
			Console.WriteLine($"Finished evaluating facts in {stopwatch.Elapsed.TotalSeconds:F1} s");
			using (var writer = new StreamWriter(_configuration.Output))
			{
				string json = JsonSerializer.Serialize(_configuration);
				writer.WriteLine("Configuration used:");
				writer.WriteLine(json);
				writer.WriteLine(string.Empty);
				var writeStats = (IOrderedEnumerable<KeyValuePair<string, StatsAggregator>> orderedFacts) =>
				{
					int rank = 1;
					foreach (var pair in orderedFacts)
					{
						string fact = pair.Key;
						var aggregator = pair.Value;
						var frequency = (decimal)aggregator.Count / performanceData.Count;
						if (frequency < _configuration.MinimumFrequency.Value)
							continue;
						string unit = units[fact];
						writer.WriteLine($"{rank}. {fact} ({unit}): μ = {aggregator.Mean:F3}, σ = {aggregator.StandardDeviation:F3} ({aggregator.Count}, {frequency:P2})");
						rank++;
					}
				};
				writer.WriteLine("Results ordered by μ:");
				writeStats(commonFacts.OrderByDescending(pair => pair.Value.Mean));
				writer.WriteLine(string.Empty);
				writer.WriteLine("Results ordered by σ:");
				writeStats(commonFacts.OrderBy(pair => pair.Value.StandardDeviation));
			}
			Console.WriteLine($"Wrote results to \"{_configuration.Output}\"");
		}
	}
}
