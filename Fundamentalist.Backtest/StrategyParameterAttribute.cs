using System.Windows.Controls;

namespace Fundamentalist.Backtest
{
	internal class StrategyParameterAttribute : Attribute
	{
		public abstract object GetValue(Control control);
	}
}
