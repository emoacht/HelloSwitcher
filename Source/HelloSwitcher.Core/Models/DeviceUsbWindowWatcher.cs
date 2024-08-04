using System;
using System.Windows.Forms;

namespace HelloSwitcher.Models;

public class DeviceUsbWindowWatcher : IDisposable
{
	#region Type

	private class MessageOnlyWindowListener : NativeWindow
	{
		private readonly DeviceUsbWindowWatcher _container;

		public MessageOnlyWindowListener(DeviceUsbWindowWatcher container)
		{
			this._container = container;

			// Create message-only window.
			// These parameters are based on TimerNativeWindow at:
			// https://referencesource.microsoft.com/#System.Windows.Forms/winforms/Managed/System/WinForms/Timer.cs
			this.CreateHandle(new CreateParams
			{
				Style = 0,
				ExStyle = 0,
				ClassStyle = 0,
				Caption = GetType().Name,
				Parent = new IntPtr(-3) // HWND_MESSAGE
			});
		}

		protected override void WndProc(ref Message m)
		{
			_container.WndProc(ref m);

			base.WndProc(ref m);
		}

		public void Close() => this.DestroyHandle();
	}

	#endregion

	private readonly MessageOnlyWindowListener _listener;
	private readonly IntPtr _notificationHandle;

	public DeviceUsbWindowWatcher()
	{
		_listener = new MessageOnlyWindowListener(this);
		_notificationHandle = DeviceUsbHelper.RegisterWindowNotification(_listener.Handle);
	}

	public event EventHandler<(string deviceName, bool exists)> UsbDeviceChanged;

	private void WndProc(ref Message m)
	{
		switch (m.Msg)
		{
			case DeviceUsbHelper.WM_DEVICECHANGE:
				switch (m.WParam.ToInt32())
				{
					case DeviceUsbHelper.DBT_DEVICEARRIVAL:
						RaiseUsbDeviceChanged(m.LParam, true);
						break;

					case DeviceUsbHelper.DBT_DEVICEREMOVECOMPLETE:
						RaiseUsbDeviceChanged(m.LParam, false);
						break;
				}
				break;
		}

		void RaiseUsbDeviceChanged(IntPtr LParam, bool exists)
		{
			if (DeviceUsbHelper.TryGetDeviceName(LParam, out string deviceName))
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
			_listener?.Close();
		}

		// Free any unmanaged objects here.
		_isDisposed = true;
	}

	#endregion
}