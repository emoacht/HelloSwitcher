using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using HelloSwitcher.ViewModels;

namespace HelloSwitcher.Views
{
	public partial class SettingsWindow : Window
	{
		private readonly SettingsWindowViewModel _viewModel;

		public SettingsWindow(App app)
		{
			InitializeComponent();

			this.DataContext = _viewModel = new SettingsWindowViewModel(app);
			this.Loaded += OnLoaded;
		}

		private async void OnLoaded(object sender, RoutedEventArgs e)
		{
			await SearchAsync();
		}

		internal Task SearchAsync() => _viewModel.SearchAsync();

		public async void Apply()
		{
			await _viewModel.ApplyAsync();
			this.Close();
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			if (!e.Cancel)
			{
				_viewModel.Dispose();
			}

			base.OnClosing(e);
		}
	}
}