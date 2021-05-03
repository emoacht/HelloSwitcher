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
		private readonly Logger _logger;

		public DeviceSwitcher(Settings settings, Logger logger)
		{
			this._settings = settings ?? throw new ArgumentNullException(nameof(settings));
			this._logger = logger;
		}

		public bool RemovableCameraExists => _removableCameraExists.GetValueOrDefault();
		private bool? _removableCameraExists = null;

		private readonly object _lock = new object();

		public Task CheckAsync(string actionName, CancellationToken cancellationToken = default) => CheckAsync(actionName, null, false, cancellationToken);

		public async Task CheckAsync(string actionName, string deviceName, bool exists, CancellationToken cancellationToken = default)
		{
			var result = new List<string> { actionName };
			void RecordResult() => _logger?.RecordOperation(string.Join(Environment.NewLine, result));

			result.Add($"deviceName: [{deviceName}], exists: {exists}");

			lock (_lock)
			{
				if ((deviceName is not null) && (_settings.RemovableCameraVidPid?.IsValid is true))
				{
					result.Add($"RemovableCameraVidPid: [{_settings.RemovableCameraVidPid}]");

					if (!_settings.RemovableCameraVidPid.Equals(new VidPid(deviceName)))
					{
						RecordResult();
						return;
					}
				}
				else
				{
					result.Add($"RemovableCameraClassGuid: {_settings.RemovableCameraClassGuid}, RemovableCameraDeviceInstanceId: [{_settings.RemovableCameraDeviceInstanceId}]");

					exists = DeviceUsbHelper.UsbCameraExists(_settings.RemovableCameraClassGuid, _settings.RemovableCameraDeviceInstanceId);
				}

				result.Add($"removableCameraExists: [{_removableCameraExists}], exists: {exists}");

				if (_removableCameraExists == exists)
				{
					RecordResult();
					return;
				}

				_removableCameraExists = exists;
			}

			if (cancellationToken.IsCancellationRequested)
			{
				RecordResult();
				return;
			}

			if (!_removableCameraExists.Value)
				result.AddRange(await PnpUtility.EnableAsync(_settings.BuiltInCameraDeviceInstanceId, cancellationToken));
			else
				result.AddRange(await PnpUtility.DisableAsync(_settings.BuiltInCameraDeviceInstanceId, cancellationToken));

			RecordResult();
		}

		public Task EnableAsync() => PnpUtility.EnableAsync(_settings.BuiltInCameraDeviceInstanceId);
		public Task DisableAsync() => PnpUtility.DisableAsync(_settings.BuiltInCameraDeviceInstanceId);
	}
}