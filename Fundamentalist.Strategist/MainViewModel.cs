using Fundamentalist.Backtest;
using Fundamentalist.Backtest.Strategies;
using System.Collections.ObjectModel;

namespace Fundamentalist.Strategist
{
	internal class MainViewModel
	{
		public ObservableCollection<Strategy> Strategies { get; set; } = new ObservableCollection<Strategy>();
		public ObservableCollection<string> Test { get; set; } = new ObservableCollection<string>();

		public MainViewModel()
		{
			Strategies.Add(new ClenowMomentumStrategy(new ClenowMomentumConfiguration()));
			Test.Add("WEFEF");
		}
	}
}
