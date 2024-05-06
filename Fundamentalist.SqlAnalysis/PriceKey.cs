namespace Fundamentalist.SqlAnalysis
{
	internal class PriceKey
	{
		private string _symbol;
		private DateTime _filed;

		public PriceKey(string symbol, DateTime filed)
		{
			_symbol = symbol;
			_filed = filed;
		}

		public override int GetHashCode()
		{
			var obj = new
			{
				_symbol,
				_filed
			};
			return obj.GetHashCode();
		}

		public override bool Equals(object? obj)
		{
			var other = obj as PriceKey;
			if (other == null)
				return false;
			return
				_symbol == other._symbol &&
				_filed == other._filed;
		}

		public override string ToString()
		{
			return $"{_symbol} {_filed}";
		}
	}
}
