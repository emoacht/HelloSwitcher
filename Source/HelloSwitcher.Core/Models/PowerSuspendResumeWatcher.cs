using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace HelloSwitcher.Models;

public class PowerSuspendResumeWatcher : IDisposable
{
	#region Win32

	[DllImport("Powrprof.dll")]
	private static extern uint PowerRegisterSuspendResumeNotification(
		uint flags,
		in DEVICE_NOTIFY_SUBSCRIBE_PARAMETERS recipient,
		out IntPtr registrationHandle);

	[DllImport("Powrprof.dll")]
	private static extern uint PowerUnregisterSuspendResumeNotification(
		IntPtr registrationHandle);

	private const uint DEVICE_NOTIFY_CALLBACK = 2;

	[StructLayout(LayoutKind.Sequential)]
	private struct DEVICE_NOTIFY_SUBSCRIBE_PARAMETERS
	{
		public DeviceNotifyCallbackRoutine callback;
		public IntPtr context;
	}

	private delegate uint DeviceNotifyCallbackRoutine(
		IntPtr context,
		int type,
		IntPtr setting);

	private const uint ERROR_SUCCESS = 0;

	#endregion

	private DEVICE_NOTIFY_SUBSCRIBE_PARAMETERS _recipient;
	private IntPtr _registrationHandle;

	public PowerSuspendResumeWatcher()
	{
		_recipient = new DEVICE_NOTIFY_SUBSCRIBE_PARAMETERS
		{
			callback = new DeviceNotifyCallbackRoutine(DeviceNotifyCallback),
			context = IntPtr.Zero
		};
		uint result = PowerRegisterSuspendResumeNotification(
			DEVICE_NOTIFY_CALLBACK,
			in _recipient,
			out _registrationHandle);
		if (result != ERROR_SUCCESS)
		{
			Debug.WriteLine($"Failed to register suspend resume notification. ({result})");
		}
	}

	private uint DeviceNotifyCallback(IntPtr context, int type, IntPtr setting)
	{
		if (Enum.IsDefined(typeof(PowerStatus), type))
		{
			PowerStatusChanged?.Invoke(this, (PowerStatus)type);
		}
		return 0;
	}

	public event EventHandler<PowerStatus> PowerStatusChanged;

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
			PowerStatusChanged = null;

			if (_registrationHandle != IntPtr.Zero)
			{
				uint result = PowerUnregisterSuspendResumeNotification(_registrationHandle);
				if (result != ERROR_SUCCESS)
				{
					Debug.WriteLine($"Failed to unregister suspend resume notification. ({result})");
				}
			}
		}

		// Free any unmanaged objects here.
		_isDisposed = true;
	}

	#endregion
}

public enum PowerStatus
{
	/// <summary>
	/// PBT_APMQUERYSUSPEND
	/// </summary>
	QuerySuspend = 0x0000,

	/// <summary>
	/// PBT_APMQUERYSUSPENDFAILED
	/// </summary>
	QuerySuspendFailed = 0x0002,

	/// <summary>
	/// PBT_APMSUSPEND
	/// </summary>
	Suspend = 0x0004,

	/// <summary>
	/// PBT_APMRESUMECRITICAL
	/// </summary>
	ResumeCritical = 0x0006,

	/// <summary>
	/// PBT_APMRESUMESUSPEND
	/// </summary>
	ResumeSuspend = 0x0007,

	/// <summary>
	/// PBT_APMBATTERYLOW
	/// </summary>
	BatteryLow = 0x0009,

	/// <summary>
	/// PBT_APMPOWERSTATUSCHANGE
	/// </summary>
	PowerStatusChange = 0x000A,

	/// <summary>
	/// PBT_APMOEMEVENT
	/// </summary>
	OemEvent = 0x000B,

	/// <summary>
	/// PBT_APMRESUMEAUTOMATIC
	/// </summary>
	ResumeAutomatic = 0x0012,

	/// <summary>
	/// PBT_POWERSETTINGCHANGE
	/// </summary>
	PowerSettingChange = 0x8013
}