namespace Fundamentalist.Trainer
{
	internal class DataPointKey
	{
		public string Ticker { get; set; }
		public DateOnly Date { get; set; }

		public DataPointKey(DataPoint dataPoint)
		{
			Ticker = dataPoint.Ticker;
			Date = dataPoint.Date;
		}

		public override int GetHashCode()
		{
			var obj = new
			{
				Ticker,
				Date
			};
			return obj.GetHashCode();
		}

		public override bool Equals(object? obj)
		{
			var key = obj as DataPointKey;
			if (key == null)
				return false;
			return
				Ticker == key.Ticker &&
				Date == key.Date;
		}

		public override string ToString()
		{
			return $"{Ticker} {Date}";
		}
	}
}
