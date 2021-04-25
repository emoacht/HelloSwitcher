using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HelloSwitcher.Models
{
	public class DeviceSwitcher
	{
		private readonly Settings _settings;

		public DeviceSwitcher(Settings settings)
		{
			this._settings = settings ?? throw new ArgumentNullException(nameof(settings));
		}

		public bool RemovableCameraExists => _removableCameraExists.GetValueOrDefault();
		private bool? _removableCameraExists = null;

		private readonly object _lock = new object();

		public Task CheckAsync() => CheckAsync(null, false);

		public async Task CheckAsync(string deviceName, bool exists)
		{
			lock (_lock)
			{
				if ((deviceName is not null) && (_settings.RemovableCameraVidPid?.IsValid is true))
				{
					if (!_settings.RemovableCameraVidPid.Equals(new VidPid(deviceName)))
						return;
				}
				else
				{
					exists = DeviceUsbHelper.UsbCameraExists(_settings.RemovableCameraClassGuid, _settings.RemovableCameraDeviceInstanceId);
				}

				if (_removableCameraExists == exists)
					return;

				_removableCameraExists = exists;
			}

			if (!_removableCameraExists.Value)
				await EnableAsync();
			else
				await DisableAsync();
		}

		public Task EnableAsync() => PnpUtility.EnableAsync(_settings.BuiltInCameraDeviceInstanceId);
		public Task DisableAsync() => PnpUtility.DisableAsync(_settings.BuiltInCameraDeviceInstanceId);
	}
}