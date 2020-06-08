using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HelloSwitcher
{
	internal static class Camera
	{
		private const string CameraFileName = "camera.txt";
		private const string BuiltinCameraIdKey = "BuiltinCameraId=";
		private const string UsbCameraIdKey = "UsbCameraId=";

		public static async Task<(bool success, string builtinCameraId, string usbCameraId)> ReadAsync(string[] args)
		{
			var (success, builtinCameraId, usbCameraId) = (2 <= args.Length)
				? (true, args[0], args[1])
				: await ReadAsync();

			return success && TryFindVidAndPid(usbCameraId, out string vidandpid)
				? (true, builtinCameraId, $@"USB\{vidandpid}")
				: (false, null, null);

			static bool TryFindVidAndPid(string source, out string vidandpid)
			{
				var pattern = new Regex("VID_[0-9a-fA-F]{4}&PID_[0-9a-fA-F]{4}");
				var match = pattern.Match(source);
				if (match.Success)
				{
					vidandpid = match.Value;
					return true;
				}
				vidandpid = default;
				return false;
			}
		}

		public static async Task<(bool success, string builtinCameraId, string usbCameraId)> ReadAsync()
		{
			var folderPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			var filePath = Path.Combine(folderPath, CameraFileName);

			if (!File.Exists(filePath))
				return (false, null, null);

			string content = null;
			try
			{
				content = await Task.Run(() => File.ReadAllText(filePath));
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex);
			}

			if (string.IsNullOrEmpty(content))
				return (false, null, null);

			var lines = content.Split(new[] { "\r\n", "\r" }, StringSplitOptions.RemoveEmptyEntries).ToArray();

			var success = TryFindValue(lines, BuiltinCameraIdKey, out string builtinDeviceId)
						& TryFindValue(lines, UsbCameraIdKey, out string usbDeviceId);

			return (success, builtinDeviceId, usbDeviceId);

			static bool TryFindValue(string[] lines, string key, out string value)
			{
				foreach (var line in lines)
				{
					if (line.StartsWith(key, StringComparison.OrdinalIgnoreCase))
					{
						var buffer = line.Substring(key.Length).Trim();
						if (!string.IsNullOrEmpty(buffer))
						{
							value = buffer;
							return true;
						}
					}
				}
				value = default;
				return false;
			}
		}
	}
}