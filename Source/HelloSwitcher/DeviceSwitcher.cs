using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using HelloSwitcher.Models;

namespace HelloSwitcher
{
	public class DeviceSwitcher
	{
		public string BuiltinDeviceId { get; }
		public string UsbDeviceId { get; }
		private string UsbDeviceName { get; }

		public DeviceSwitcher(string builtinDeviceId, string usbDeviceId)
		{
			this.BuiltinDeviceId = builtinDeviceId ?? throw new ArgumentNullException(nameof(builtinDeviceId));
			this.UsbDeviceId = usbDeviceId ?? throw new ArgumentNullException(nameof(usbDeviceId));
			UsbDeviceName = usbDeviceId.Replace(@"USB\", "USB#");
		}

		public bool UsbDeviceExists => _usbDeviceExists.GetValueOrDefault();
		private bool? _usbDeviceExists = null;

		private readonly object _lock = new object();

		public Task CheckAsync() => CheckAsync(null, false);

		public async Task CheckAsync(string deviceName, bool exists)
		{
			lock (_lock)
			{
				if ((deviceName is not null) && (UsbDeviceName is not null))
				{
					if (deviceName.IndexOf(UsbDeviceName, StringComparison.OrdinalIgnoreCase) < 0)
						return;
				}
				else
				{
					var result = SetupUsbHelper.UsbDeviceExists(UsbDeviceId);
					if (!result.HasValue)
						return;

					exists = result.Value;
				}

				if (_usbDeviceExists == exists)
					return;

				_usbDeviceExists = exists;
			}

			if (!_usbDeviceExists.Value)
				await EnableAsync();
			else
				await DisableAsync();
		}

		public Task<bool> EnableAsync() => DeviceConsole.EnableAsync(BuiltinDeviceId);
		public Task<bool> DisableAsync() => DeviceConsole.DisableAsync(BuiltinDeviceId);
	}
}