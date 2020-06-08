using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace HelloSwitcher
{
	public partial class App : Application
	{
		private DeviceSwitcher _switcher;
		private NotifyIconHolder _holder;

		protected override async void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			DispatcherUnhandledException += (_, e) => Record(e.Exception);
			TaskScheduler.UnobservedTaskException += (_, e) => Record(e.Exception);
			AppDomain.CurrentDomain.UnhandledException += (_, e) => Record(e.ExceptionObject);

			var (success, builtinCameraId, usbCameraId) = await Camera.ReadAsync(e.Args);
			if (!success)
			{
				this.Shutdown();
				return;
			}

			_switcher = new DeviceSwitcher(builtinCameraId, usbCameraId);
			await _switcher.CheckAsync();

			_holder = new NotifyIconHolder(
				new[]
				{
					"pack://application:,,,/HelloSwitcher;component/Resources/Camera_absent.ico",
					"pack://application:,,,/HelloSwitcher;component/Resources/Camera_present.ico",
				},
				Convert.ToInt32(_switcher.UsbDeviceExists),
				"Hello Switcher",
				new[]
				{
					(ToolStripItemType.Label, "Hello Switcher", null),
					(ToolStripItemType.Separator, null, null),
					(ToolStripItemType.Button, "Re-check USB camera", async () => await _switcher.CheckAsync()),
					(ToolStripItemType.Button, "Enable built-in camera", async () => await _switcher.EnableAsync()),
					(ToolStripItemType.Button, "Disable built-in camera", async () => await _switcher.DisableAsync()),
					(ToolStripItemType.Separator, null, null),
					(ToolStripItemType.Button,"Close", new Action(async () =>
					{
						await _switcher.EnableAsync();
						this.Shutdown();
					}))
				});

			_holder.UsbDeviceChanged += async (_, e) =>
			{
				await _switcher.CheckAsync(e.deviceName, e.exists);
				_holder.IconIndex = Convert.ToInt32(_switcher.UsbDeviceExists);
			};
		}

		private void Record(object exception)
		{
			SystemSounds.Exclamation.Play();

			var folderPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			var filePath = Path.Combine(folderPath, "exception.txt");

			File.WriteAllText(filePath, $"[{DateTime.Now}]\r\n{exception}");
		}

		protected override void OnExit(ExitEventArgs e)
		{
			_holder?.Dispose();

			base.OnExit(e);
		}
	}
}