using Fundamentalist.Backtest;
using Fundamentalist.Backtest.Strategies;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace Fundamentalist.Strategist
{
	public partial class MainWindow : Window
	{
		public ObservableCollection<Strategy> Strategies { get; set; }

		public MainWindow()
		{
			Strategies = new ObservableCollection<Strategy>();
			Strategies.Add(new ClenowMomentumStrategy(new ClenowMomentumConfiguration()));
			InitializeComponent();
		}

		private void OnDatePreviewTextInput(object sender, TextCompositionEventArgs e)
		{
			var pattern = new Regex("^[0-9\\-:]+$");
			e.Handled = !pattern.IsMatch(e.Text);
		}

		private void OnNumericPreviewTextInput(object sender, TextCompositionEventArgs e)
		{
			var pattern = new Regex("^[0-9]+\\.?[0-9]*$");
			e.Handled = !pattern.IsMatch(e.Text);
		}
	}
}