using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace HelloSwitcher.Service
{
	public static class ServiceBroker
	{
		public const string ServiceName = "HelloSwitcherService";

		private static bool TryGetService(out ServiceController sc)
		{
			try
			{
				sc = ServiceController.GetServices().FirstOrDefault(x => x.ServiceName == ServiceName);
				return (sc is not null);
			}
			catch (Win32Exception)
			{
				sc = default;
				return false;
			}
		}

		#region Install/Uninstall

		/// <summary>
		/// Determines if the service is installed.
		/// </summary>
		/// <returns>True if installed. False otherwise.</returns>
		public static bool IsInstalled() => TryGetService(out _);

		private static string ServiceFilePath => Assembly.GetExecutingAssembly().Location;

		/// <summary>
		/// Installs the service if not installed (without generating log).
		/// </summary>
		/// <param name="arguments">Command-line arguments to the service (not to installer)</param>
		public static void Install(string arguments = null)
		{
			if (IsInstalled())
				return;

			try
			{
				ManagedInstallerClass.InstallHelper(new[] { "/LogFile=", $"{ProjectInstaller.ArgumentsOption}{arguments}", ServiceFilePath });
			}
			catch (InvalidOperationException)
			{
			}
		}

		/// <summary>
		/// Uninstalls the service if installed (without generating log).
		/// </summary>
		public static void Uninstall()
		{
			if (!IsInstalled())
				return;

			try
			{
				ManagedInstallerClass.InstallHelper(new[] { "/LogFile=", "/u", ServiceFilePath });
			}
			catch (InvalidOperationException)
			{
			}
		}

		#endregion

		#region Start/Stop/Pause/Continue

		/// <summary>
		/// Determines if the service is running.
		/// </summary>
		/// <returns>True if running. False if not installed or not running.</returns>
		public static bool IsRunning()
		{
			return TryGetService(out ServiceController sc)
				&& (sc.Status == ServiceControllerStatus.Running);
		}

		/// <summary>
		/// Starts the service. If the service is running, stops and then starts the service.
		/// </summary>
		/// <param name="timeout">Timeout duration</param>
		/// <returns>True if successfully started. False if not installed or failed.</returns>
		public static async Task<bool> RestartAsync(TimeSpan timeout)
		{
			if (!TryGetService(out ServiceController sc))
				return false;

			try
			{
				if (sc.Status is not ServiceControllerStatus.Stopped)
				{
					sc.Stop();
					if (!await sc.WaitForStatusAsync(ServiceControllerStatus.Stopped, timeout))
						return false;
				}

				sc.Start();
				return await sc.WaitForStatusAsync(ServiceControllerStatus.Running, timeout);
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Failed to start the service.\r\n{ex}");
				return false;
			}
		}

		/// <summary>
		/// Stops the service.
		/// </summary>
		/// <param name="timeout">Timeout duration</param>
		/// <returns>True if successfully stopped. False if not installed or failed.</returns>
		public static async Task<bool> StopAsync(TimeSpan timeout)
		{
			if (!TryGetService(out ServiceController sc))
				return false;

			try
			{
				if (sc.Status is ServiceControllerStatus.Stopped)
					return false;

				sc.Stop();
				return await sc.WaitForStatusAsync(ServiceControllerStatus.Stopped, timeout);
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Failed to stop the service.\r\n{ex}");
				return false;
			}
		}

		public static void Pause()
		{
			if (!TryGetService(out ServiceController sc))
				return;

			try
			{
				switch (sc.Status)
				{
					case ServiceControllerStatus.Running:
					case ServiceControllerStatus.StartPending:
						sc.Pause();
						break;
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Failed to pause the service.\r\n{ex}");
			}
		}

		public static void Continue()
		{
			if (!TryGetService(out ServiceController sc))
				return;

			try
			{
				switch (sc.Status)
				{
					case ServiceControllerStatus.Paused:
					case ServiceControllerStatus.PausePending:
						sc.Continue();
						break;

					case ServiceControllerStatus.Stopped:
					case ServiceControllerStatus.StopPending:
						sc.Start();
						break;
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Failed to continue the service.\r\n{ex}");
			}
		}

		private static async Task<bool> WaitForStatusAsync(this ServiceController sc, ServiceControllerStatus desiredStatus, TimeSpan timeout)
		{
			if (!Enum.IsDefined(typeof(ServiceControllerStatus), desiredStatus))
				throw new ArgumentException(nameof(desiredStatus));

			var dueTime = DateTime.UtcNow + timeout;
			sc.Refresh();

			while (sc.Status != desiredStatus)
			{
				if (DateTime.UtcNow > dueTime)
					return false;

				await Task.Delay(250);
				sc.Refresh();
			}
			return true;
		}

		#endregion
	}
}