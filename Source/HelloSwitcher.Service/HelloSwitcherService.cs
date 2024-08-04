using System;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;

using HelloSwitcher.Helper;
using HelloSwitcher.Models;

namespace HelloSwitcher.Service;

public partial class HelloSwitcherService : ServiceBase
{
	internal Settings Settings { get; }
	internal Logger Logger { get; }

	private DeviceSwitcher _switcher;

	internal bool IsPaused { get; private set; }

	private readonly Sample _check;
	private CancellationTokenSource _checkTokenSource;
	private IntPtr _notificationHandle;

	public HelloSwitcherService()
	{
		InitializeComponent();

		TaskScheduler.UnobservedTaskException += (_, e) => Logger.RecordError(e.Exception);
		AppDomain.CurrentDomain.UnhandledException += (_, e) => Logger.RecordError(e.ExceptionObject);

		Settings = new Settings();
		Logger = new Logger("operation.service.log", "error.service.log");

		_check = new Sample(
			TimeSpan.FromSeconds(1),
			(actionName, cancellationToken) => _switcher?.CheckAsync(actionName, cancellationToken));
	}

	protected override async void OnStart(string[] args)
	{
		Logger.RecordOperation("Start");

		await Settings.LoadAsync();

		Logger.RecordOperation($"Settings IsLoaded: {Settings.IsLoaded}");

		_checkTokenSource?.Dispose();
		_checkTokenSource = new();

		_switcher = new DeviceSwitcher(Settings, Logger);
		await _switcher.CheckAsync("Initial Check", _checkTokenSource.Token);

		DeviceUsbHelper.UnregisterNotification(_notificationHandle);
		_notificationHandle = DeviceUsbHelper.RegisterServiceNotification(this.ServiceHandle);
	}

	protected override void OnStop()
	{
		Logger.RecordOperation("Stop");

		_checkTokenSource?.Cancel();
		_checkTokenSource?.Dispose();
		_checkTokenSource = null;

		DeviceUsbHelper.UnregisterNotification(_notificationHandle);
		_notificationHandle = IntPtr.Zero;
	}

	protected override void OnPause()
	{
		Logger.RecordOperation("Pause");

		IsPaused = true;
	}

	protected override async void OnContinue()
	{
		Logger.RecordOperation("Continue");

		await Settings.LoadAsync();
		IsPaused = false;
	}

	protected override bool OnPowerEvent(PowerBroadcastStatus powerStatus)
	{
		Logger.RecordOperation($"Power Changed ({powerStatus}){(IsPaused ? " [Paused]" : string.Empty)}");

		if (!IsPaused)
		{
			switch (powerStatus)
			{
				case PowerBroadcastStatus.ResumeAutomatic:
				case PowerBroadcastStatus.ResumeSuspend:
					_check.Push($"Power Changed Check", _checkTokenSource?.Token ?? default);
					break;
			}
		}
		return true;
	}

	/// <summary>
	/// Occurs when a custom command is received.
	/// </summary>
	/// <param name="command">Command (control code)</param>
	/// <remarks>
	/// Actually, this method is also called when a command (control code) other than those for
	/// ordinary service sessions as well as CONTROL_POWEREVENT, CONTROL_SESSIONCHANGE is received
	/// if the service is configured to receive such a command.
	/// A custom command (user-defined control code) must range from 128 to 255.
	/// https://docs.microsoft.com/en-us/windows/win32/api/winsvc/nc-winsvc-lphandler_function_ex
	/// </remarks>
	protected override void OnCustomCommand(int command)
	{
		switch (command)
		{
			case DeviceUsbHelper.SERVICE_CONTROL_DEVICEEVENT:
				Logger.RecordOperation($"Device Changed{(IsPaused ? " [Paused]" : string.Empty)}");

				if (!IsPaused)
				{
					_check.Push($"Device Changed Check", _checkTokenSource?.Token ?? default);
				}
				break;
		}
	}
}