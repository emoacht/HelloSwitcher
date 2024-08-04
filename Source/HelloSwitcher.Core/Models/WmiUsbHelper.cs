using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;

namespace HelloSwitcher.Models;

/// <summary>
/// Utility methods for USB devices by WMI
/// </summary>
public class WmiUsbHelper : IDisposable
{
	#region Static methods

	public static IEnumerable<string> EnumerateUsbDeviceIds()
	{
		using var searcher = new ManagementObjectSearcher(@"Select * From Win32_USBHub");
		ManagementObjectCollection collection = null;
		try
		{
			collection = searcher.Get();
		}
		catch (Exception ex)
		{
			Debug.WriteLine(ex);
		}

		if (collection is null)
			yield break;

		using (collection)
		{
			foreach (var device in collection)
			{
				var deviceId = (string)device.GetPropertyValue("DeviceID");
				if (!string.IsNullOrWhiteSpace(deviceId))
					yield return deviceId;
			}
		}
	}

	public static bool CheckUsbDeviceExists(string deviceId)
	{
		try
		{
			return EnumerateUsbDeviceIds().Any(x => x.StartsWith(deviceId, StringComparison.Ordinal));
		}
		catch (ManagementException ex)
		{
			Debug.WriteLine(ex);
			return false;
		}
	}

	#endregion

	public event EventHandler<bool> UsbDeviceChanged;

	public bool UsbDeviceExists
	{
		get => _usbDeviceExists;
		set
		{
			if (_usbDeviceExists == value)
				return;

			_usbDeviceExists = value;
			UsbDeviceChanged?.Invoke(this, value);
		}
	}
	private bool _usbDeviceExists;

	public bool CheckUsbDeviceExists() => UsbDeviceExists = CheckUsbDeviceExists(_deviceId);

	private readonly string _deviceId;
	private readonly ManagementEventWatcher _attachedWatcher;
	private readonly ManagementEventWatcher _detachedWatcher;

	public WmiUsbHelper(string deviceId)
	{
		this._deviceId = deviceId ?? throw new ArgumentNullException(nameof(deviceId));
		CheckUsbDeviceExists();

		var attachedQuery = new WqlEventQuery("SELECT * FROM __InstanceCreationEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_USBHub'");
		_attachedWatcher = new ManagementEventWatcher(attachedQuery);
		_attachedWatcher.EventArrived += OnAttached;
		_attachedWatcher.Start();

		var detachedQuery = new WqlEventQuery("SELECT * FROM __InstanceDeletionEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_USBHub'");
		_detachedWatcher = new ManagementEventWatcher(detachedQuery);
		_detachedWatcher.EventArrived += OnDetached;
		_detachedWatcher.Start();
	}

	private void OnAttached(object sender, EventArrivedEventArgs e)
	{
		if (IsDeviceIdContained(e))
			UsbDeviceExists = true;
	}

	private void OnDetached(object sender, EventArrivedEventArgs e)
	{
		if (IsDeviceIdContained(e))
			UsbDeviceExists = false;
	}

	private bool IsDeviceIdContained(EventArrivedEventArgs e)
	{
		var instance = (ManagementBaseObject)e.NewEvent["TargetInstance"];
		var deviceId = (string)instance.GetPropertyValue("DeviceID");
		return !string.IsNullOrEmpty(deviceId)
			&& deviceId.StartsWith(this._deviceId, StringComparison.Ordinal);
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
			UsbDeviceChanged = null;

			// Free any other managed objects here.
			_attachedWatcher.EventArrived -= OnAttached;
			_attachedWatcher.Stop();
			_attachedWatcher.Dispose();

			_detachedWatcher.EventArrived -= OnDetached;
			_detachedWatcher.Stop();
			_detachedWatcher.Dispose();
		}

		// Free any unmanaged objects here.
		_isDisposed = true;
	}

	#endregion
}