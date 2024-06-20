using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Fundamentalist.Strategist
{
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
			DataContext = new MainViewModel();
			Strategy.SelectedIndex = 0;
			TextBox
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