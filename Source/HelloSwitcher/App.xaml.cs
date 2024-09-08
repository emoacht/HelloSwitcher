using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

using HelloSwitcher.Models;
using HelloSwitcher.Service;
using HelloSwitcher.Views;

namespace HelloSwitcher;

public partial class App : Application
{
	internal Settings Settings { get; }
	internal Logger Logger { get; }

	private DeviceSwitcher _switcher;
	private DeviceUsbWindowWatcher _deviceWatcher;
	private PowerSuspendResumeWatcher _powerWatcher;
	private NotifyIconHolder _holder;

	internal static bool IsInteractive { get; } = Environment.UserInteractive;

	public App() : base()
	{
		DispatcherUnhandledException += (_, e) => Logger.RecordError(e.Exception);
		TaskScheduler.UnobservedTaskException += (_, e) => Logger.RecordError(e.Exception);
		AppDomain.CurrentDomain.UnhandledException += (_, e) => Logger.RecordError(e.ExceptionObject);

		Settings = new Settings();
		Logger = new Logger("operation.log", "error.log");
	}

	protected override async void OnStartup(StartupEventArgs e)
	{
		base.OnStartup(e);

		Logger.RecordOperation("Start");

		if (!Initiate())
		{
			this.Shutdown();
			return;
		}

		await Settings.LoadAsync();

		Logger.RecordOperation($"Settings IsLoaded: {Settings.IsLoaded}");

		_switcher = new DeviceSwitcher(Settings, Logger);
		await _switcher.CheckAsync("Initial Check");

		_deviceWatcher = new DeviceUsbWindowWatcher();
		_deviceWatcher.UsbDeviceChanged += async (_, e) =>
		{
			await _switcher.CheckAsync("Device Changed Check", e.deviceName, e.exists);

			if (IsInteractive)
			{
				_holder?.UpdateIcon(_switcher.RemovableCameraExists);
				await UpdateSettings();
			}
		};

		_powerWatcher = new PowerSuspendResumeWatcher();
		_powerWatcher.PowerStatusChanged += async (_, status) =>
		{
			switch (status)
			{
				case PowerStatus.ResumeAutomatic:
				case PowerStatus.ResumeSuspend:
					await _switcher.CheckAsync($"Resumed Check ({status})");
					break;
			}
		};

		if (IsInteractive)
		{
			_holder = new NotifyIconHolder(
				[
					"pack://application:,,,/HelloSwitcher;component/Resources/Disconnected.ico",
					"pack://application:,,,/HelloSwitcher;component/Resources/Connected.ico",
				],
				"Hello Switcher");
			_holder.UpdateIcon(_switcher.RemovableCameraExists);

			_holder.MouseRightClick += (_, _) => ShowMenu();

			if (!Settings.IsLoaded)
				ShowSettings();
		}
	}

	protected override void OnExit(ExitEventArgs e)
	{
		_deviceWatcher?.Dispose();
		_powerWatcher?.Dispose();
		_holder?.Dispose();
		End();

		base.OnExit(e);
	}

	#region Lifecycle

	public const string UninstallOption = "/uninstall";

	private const string SemaphoreName = "HelloSwitcher.App";
	private Semaphore _semaphore;
	private bool _semaphoreCreated;

	internal bool RunAsService { get; set; }

	private bool Initiate()
	{
		if (Environment.GetCommandLineArgs().Any(x => string.Equals(x, UninstallOption, StringComparison.OrdinalIgnoreCase)))
		{
			ServiceBroker.Uninstall();
			return false;
		}

		try
		{
			_semaphore = new Semaphore(1, 1, SemaphoreName, out _semaphoreCreated);
		}
		catch
		{
		}

		if (!_semaphoreCreated)
			return false;

		RunAsService = ServiceBroker.IsInstalled();
		if (RunAsService)
			ServiceBroker.Pause();

		return true;
	}

	private void End()
	{
		if (!_semaphoreCreated)
			return;

		ServiceBroker.Continue();
		_semaphore?.Dispose();
	}

	#endregion

	#region Window

	private void ShowMenu()
	{
		if (this.MainWindow is not MenuWindow menuWindow)
		{
			this.MainWindow = menuWindow = new MenuWindow();
			menuWindow.Selected += OnSelected;
		}

		menuWindow.ShowContextMenu();
		menuWindow.Activate();

		async void OnSelected(object sender, MenuAction action)
		{
			switch (action)
			{
				case MenuAction.ShowSettings:
					ShowSettings();
					break;

				case MenuAction.Recheck:
					await _switcher.CheckAsync("Manual Check");
					_holder.UpdateIcon(_switcher.RemovableCameraExists);
					await UpdateSettings();
					break;

				case MenuAction.Enable:
					await _switcher.EnableAsync();
					await UpdateSettings();
					break;

				case MenuAction.Disable:
					await _switcher.DisableAsync();
					await UpdateSettings();
					break;

				case MenuAction.CloseApp:
					if (!RunAsService)
						await _switcher.EnableAsync();

					this.Shutdown();
					break;
			}
		}
	}

	private SettingsWindow _settingsWindow;

	private void ShowSettings()
	{
		if (_settingsWindow is not null)
		{
			_settingsWindow.Activate();
			return;
		}

		_settingsWindow = new SettingsWindow(this);
		_settingsWindow.Closed += OnClosed;
		_settingsWindow.Show();

		async void OnClosed(object sender, EventArgs e)
		{
			_settingsWindow = null;
			await _switcher.CheckAsync("Settings Changed Check");
			_holder.UpdateIcon(_switcher.RemovableCameraExists);

			await Task.Run(() =>
			{
				if (RunAsService)
				{
#if DEBUG
					ServiceBroker.Install(Logger.OperationOption);
#else
					ServiceBroker.Install();
#endif
				}
				else
					ServiceBroker.Uninstall();
			});
		}
	}

	private Task UpdateSettings()
	{
		return _settingsWindow?.SearchAsync() ?? Task.CompletedTask;
	}

	#endregion
}