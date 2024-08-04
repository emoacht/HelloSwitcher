using System;
using System.Runtime.InteropServices;
using System.ServiceProcess;

namespace HelloSwitcher.Models;

public class DeviceUsbServiceWatcher : IDisposable
{
	#region Win32

	[DllImport("Advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	private static extern IntPtr RegisterServiceCtrlHandlerEx(
		string lpServiceName,
		ServiceControlHandlerEx lpHandlerProc,
		IntPtr lpContext);

	private delegate int ServiceControlHandlerEx(
		int dwControl,
		int dwEventType,
		IntPtr lpEventData,
		IntPtr lpContext);

	[DllImport("Advapi32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool SetServiceStatus(
		IntPtr hServiceStatus,
		ref SERVICE_STATUS lpServiceStatus);

	[StructLayout(LayoutKind.Sequential)]
	private struct SERVICE_STATUS
	{
		public uint dwServiceType;
		public uint dwCurrentState;
		public uint dwControlsAccepted;
		public uint dwWin32ExitCode;
		public uint dwServiceSpecificExitCode;
		public uint dwCheckPoint;
		public uint dwWaitHint;
	}

	private const int SERVICE_WIN32_OWN_PROCESS = 0x00000010;

	private const uint SERVICE_STOPPED = 0x00000001;
	private const uint SERVICE_START_PENDING = 0x00000002;
	private const uint SERVICE_STOP_PENDING = 0x00000003;
	private const uint SERVICE_RUNNING = 0x00000004;
	private const uint SERVICE_CONTINUE_PENDING = 0x00000005;
	private const uint SERVICE_PAUSE_PENDING = 0x00000006;
	private const uint SERVICE_PAUSED = 0x00000007;

	private const int SERVICE_ACCEPT_STOP = 0x00000001;

	#endregion

	private readonly IntPtr _statusHandle;
	private readonly IntPtr _notificationHandle;

	public DeviceUsbServiceWatcher(string serviceName)
	{
		// This function call using ServiceBase.ServiceName causes AccessViolationException
		// when the system suspends and so make this class effectively unusable.
		// The service status handle obtained by this function matches ServiceBase.ServiceHandle
		// which is obtained by the same function inside ServiceBase class. 
		_statusHandle = RegisterServiceCtrlHandlerEx(
			serviceName,
			HandlerProc,
			IntPtr.Zero);
		if (_statusHandle != IntPtr.Zero)
		{
			_notificationHandle = DeviceUsbHelper.RegisterServiceNotification(_statusHandle);
		}
	}

	public event EventHandler<(string deviceName, bool exists)> UsbDeviceChanged;
	public event EventHandler<PowerBroadcastStatus> PowerChanged;
	public event EventHandler StopOrdered;

	private const int NO_ERROR = 0;
	private const int ERROR_CALL_NOT_IMPLEMENTED = 0x78;

	private int HandlerProc(int control, int eventType, IntPtr eventData, IntPtr context)
	{
		switch (control)
		{
			case DeviceUsbHelper.SERVICE_CONTROL_DEVICEEVENT:
				switch (eventType)
				{
					case DeviceUsbHelper.DBT_DEVICEARRIVAL:
						RaiseUsbDeviceChanged(eventData, true);
						return NO_ERROR;

					case DeviceUsbHelper.DBT_DEVICEREMOVECOMPLETE:
						RaiseUsbDeviceChanged(eventData, false);
						return NO_ERROR;
				}
				break;

			case DeviceUsbHelper.SERVICE_CONTROL_POWEREVENT:
				var powerStatus = (PowerBroadcastStatus)eventType;
				PowerChanged?.Invoke(this, powerStatus);
				return NO_ERROR;

			case DeviceUsbHelper.SERVICE_CONTROL_STOP:
				var status = new SERVICE_STATUS
				{
					dwServiceType = SERVICE_WIN32_OWN_PROCESS,
					dwCurrentState = SERVICE_STOP_PENDING,
					dwControlsAccepted = SERVICE_ACCEPT_STOP,
					dwWin32ExitCode = NO_ERROR,
					dwServiceSpecificExitCode = 0,
					dwCheckPoint = 0,
					dwWaitHint = 0
				};
				SetServiceStatus(_statusHandle, ref status);

				StopOrdered?.Invoke(this, EventArgs.Empty);

				status.dwCurrentState = SERVICE_STOPPED;
				SetServiceStatus(_statusHandle, ref status);
				return NO_ERROR;
		}
		return ERROR_CALL_NOT_IMPLEMENTED;

		void RaiseUsbDeviceChanged(IntPtr eventData, bool exists)
		{
			if (DeviceUsbHelper.TryGetDeviceName(eventData, out string deviceName))
				UsbDeviceChanged?.Invoke(this, (deviceName, exists));
		}
	}

	#region IDisposable

	private bool _isDisposed = false;

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (_isDisposed)
			return;

		if (disposing)
		{
			// Free any other managed objects here.
			DeviceUsbHelper.UnregisterNotification(_notificationHandle);
		}

		// Free any unmanaged objects here.
		_isDisposed = true;
	}

	#endregion
}