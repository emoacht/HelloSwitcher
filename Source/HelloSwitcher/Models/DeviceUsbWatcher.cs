using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HelloSwitcher.Models
{
	internal class DeviceUsbWatcher : IDisposable
	{
		#region Type

		private class MessageOnlyWindowListener : NativeWindow
		{
			private readonly DeviceUsbWatcher _container;

			public MessageOnlyWindowListener(DeviceUsbWatcher container)
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

		public DeviceUsbWatcher()
		{
			_listener = new MessageOnlyWindowListener(this);

			DeviceUsbHelper.RegisterUsbDeviceNotification(_listener.Handle);
		}

		public event EventHandler<(string deviceName, bool exists)> UsbDeviceChanged;

		private void WndProc(ref Message m)
		{
			switch (m.Msg)
			{
				case DeviceUsbHelper.WM_DEVICECHANGE:
					switch (m.WParam.ToInt32())
					{
						case DeviceUsbHelper.DBT_DEVICEREMOVECOMPLETE:
							RaiseUsbDeviceChanged(m.LParam, false);
							break;

						case DeviceUsbHelper.DBT_DEVICEARRIVAL:
							RaiseUsbDeviceChanged(m.LParam, true);
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
				DeviceUsbHelper.UnregisterUsbDeviceNotification();
				_listener?.Close();
			}

			// Free any unmanaged objects here.
			_isDisposed = true;
		}

		#endregion
	}
}