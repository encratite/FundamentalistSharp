namespace Fundamentalist.Backtest
{
	internal abstract class Strategy
	{
		public Backtest Backtest { get; set; }

		public abstract void Next();
	}
}
