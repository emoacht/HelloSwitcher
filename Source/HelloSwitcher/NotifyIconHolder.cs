using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Threading;

using HelloSwitcher.Models;

namespace HelloSwitcher
{
	public class NotifyIconHolder : IDisposable
	{
		#region Type

		private class NotifyIconWindowListener : NativeWindow
		{
			public static NotifyIconWindowListener Create(NotifyIconHolder container)
			{
				if (!NotifyIconHelper.TryGetNotifyIconWindow(container._notifyIcon, out NativeWindow window)
					|| (window.Handle == IntPtr.Zero))
				{
					return null;
				}
				return new NotifyIconWindowListener(container, window);
			}

			private readonly NotifyIconHolder _container;

			private NotifyIconWindowListener(NotifyIconHolder container, NativeWindow window)
			{
				this._container = container;
				this.AssignHandle(window.Handle);
			}

			protected override void WndProc(ref Message m)
			{
				_container.WndProc(ref m);

				base.WndProc(ref m);
			}

			public void Close() => this.ReleaseHandle();
		}

		#endregion

		private readonly NotifyIconWindowListener _listener;

		private readonly NotifyIcon _notifyIcon;
		private readonly System.Drawing.Icon[] _icons;

		public NotifyIconHolder(string[] iconUriStrings, int iconIndex, string iconText, IEnumerable<(ToolStripItemType type, string text, Action action)> menus)
		{
			_notifyIcon = new NotifyIcon();
			_notifyIcon.Text = iconText;

			_icons = iconUriStrings.Select(x =>
				{
					var iconResource = System.Windows.Application.GetResourceStream(new Uri(x));
					if (iconResource is null)
						return null;

					using var iconStream = iconResource.Stream;
					return new System.Drawing.Icon(iconStream);
				})
				.Where(x => x != null)
				.ToArray();

			this.IconIndex = iconIndex;

			_notifyIcon.ContextMenuStrip = new ContextMenuStrip();
			foreach (var (type, text, action) in menus)
			{
				switch (type)
				{
					case ToolStripItemType.Button:
						_notifyIcon.ContextMenuStrip.Items.Add(text, null, (_, __) => action?.Invoke());
						break;
					case ToolStripItemType.Label:
						_notifyIcon.ContextMenuStrip.Items.Add(new ToolStripLabel(text));
						break;
					case ToolStripItemType.Separator:
						_notifyIcon.ContextMenuStrip.Items.Add(new ToolStripSeparator());
						break;
				}
			}

			// Show NotifyIcon.
			_notifyIcon.Visible = true;

			_listener = NotifyIconWindowListener.Create(this);
			if (_listener is null)
				return;

			SetupUsbHelper.RegisterUsbDeviceNotification(_listener.Handle);
		}

		public int IconIndex
		{
			get => _iconIndex;
			set
			{
				if ((_iconIndex == value) || (_icons.Length <= value))
					return;

				_iconIndex = value;
				_notifyIcon.Icon = _icons[value];
			}
		}
		private int _iconIndex = -1; // -1 is to let 0 as the first value to set _notifyIcon.Icon.

		public event EventHandler<(string deviceName, bool exists)> UsbDeviceChanged;

		private void WndProc(ref Message m)
		{
			switch (m.Msg)
			{
				case SetupUsbHelper.WM_DEVICECHANGE:
					switch (m.WParam.ToInt32())
					{
						case SetupUsbHelper.DBT_DEVICEREMOVECOMPLETE:
							RaiseUsbDeviceChanged(m.LParam, false);
							break;

						case SetupUsbHelper.DBT_DEVICEARRIVAL:
							RaiseUsbDeviceChanged(m.LParam, true);
							break;
					}
					break;
			}

			void RaiseUsbDeviceChanged(IntPtr LParam, bool exists)
			{
				if (SetupUsbHelper.TryGetDeviceName(LParam, out string deviceName))
					UsbDeviceChanged?.Invoke(_notifyIcon, (deviceName, exists));
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
				SetupUsbHelper.UnregisterUsbDeviceNotification();
				_listener?.Close();
				_notifyIcon?.Dispose();
			}

			// Free any unmanaged objects here.
			_isDisposed = true;
		}

		#endregion
	}

	public enum ToolStripItemType
	{
		Button = 0,
		Label,
		Separator
	}
}