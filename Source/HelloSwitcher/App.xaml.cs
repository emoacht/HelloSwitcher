using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Media;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

using HelloSwitcher.Models;
using HelloSwitcher.Views;

namespace HelloSwitcher
{
	public partial class App : Application
	{
		private Settings _settings;
		private DeviceSwitcher _switcher;
		private NotifyIconHolder _holder;

		protected override async void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			DispatcherUnhandledException += (_, e) => Record(e.Exception);
			TaskScheduler.UnobservedTaskException += (_, e) => Record(e.Exception);
			AppDomain.CurrentDomain.UnhandledException += (_, e) => Record(e.ExceptionObject);

			_settings = new Settings();
			await _settings.LoadAsync();

			_switcher = new DeviceSwitcher(_settings);
			await _switcher.CheckAsync();

			_holder = new NotifyIconHolder(
				new[]
				{
					"pack://application:,,,/HelloSwitcher;component/Resources/Disconnected.ico",
					"pack://application:,,,/HelloSwitcher;component/Resources/Connected.ico",
				},
				Convert.ToInt32(_switcher.RemovableCameraExists),
				"Hello Switcher",
				new[]
				{
					(ToolStripItemType.Label, "Hello Switcher", null),
					(ToolStripItemType.Separator, null, null),
					(ToolStripItemType.Button, "Re-check USB camera", async () => await _switcher.CheckAsync()),
					(ToolStripItemType.Button, "Enable built-in camera", async () => await _switcher.EnableAsync()),
					(ToolStripItemType.Button, "Disable built-in camera", async () => await _switcher.DisableAsync()),
					(ToolStripItemType.Separator, null, null),
					(ToolStripItemType.Button,"Close", new Action(async () =>
					{
						await _switcher.EnableAsync();
						this.Shutdown();
					}))
				});

			_holder.UsbDeviceChanged += async (_, e) =>
			{
				await _switcher.CheckAsync(e.deviceName, e.exists);
				_holder.IconIndex = Convert.ToInt32(_switcher.RemovableCameraExists);
				await UpdateWindow();
			};

			if (!_settings.IsLoaded)
				ShowWindow();
		}

		private void ShowWindow()
		{
			var window = this.Windows.OfType<SettingsWindow>().FirstOrDefault();
			if (window is not null)
			{
				window.Activate();
				return;
			}

			this.MainWindow = new SettingsWindow(_settings);
			this.MainWindow.Closed += OnClosed;
			this.MainWindow.Show();

			async void OnClosed(object sender, EventArgs e)
			{
				this.MainWindow = null;
				await _switcher.CheckAsync();
				_holder.IconIndex = Convert.ToInt32(_switcher.RemovableCameraExists);
			}
		}

		private Task UpdateWindow()
		{
			var window = this.Windows.OfType<SettingsWindow>().FirstOrDefault();
			if (window is null)
				return Task.CompletedTask;

			return window.SearchAsync();
		}

		private void Record(object exception)
		{
			SystemSounds.Exclamation.Play();

			var folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), nameof(HelloSwitcher));
			if (!Directory.Exists(folderPath))
				Directory.CreateDirectory(folderPath);

			var filePath = Path.Combine(folderPath, "exception.txt");

			File.WriteAllText(filePath, $"[{DateTime.Now}]\r\n{exception}");
		}

		protected override void OnExit(ExitEventArgs e)
		{
			_holder?.Dispose();

			base.OnExit(e);
		}
	}
}