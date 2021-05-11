using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace HelloSwitcher
{
	internal static class WindowHelper
	{
		#region Win32

		[DllImport("User32.dll", SetLastError = true)]
		private static extern IntPtr FindWindowEx(
			IntPtr hwndParent,
			IntPtr hwndChildAfter,
			string lpszClass,
			string lpszWindow);

		[DllImport("User32.dll")]
		private static extern IntPtr MonitorFromWindow(
			IntPtr hwnd,
			MONITOR_DEFAULTTO dwFlags);

		private enum MONITOR_DEFAULTTO : uint
		{
			MONITOR_DEFAULTTONULL = 0x00000000,
			MONITOR_DEFAULTTOPRIMARY = 0x00000001,
			MONITOR_DEFAULTTONEAREST = 0x00000002,
		}

		[DllImport("Shcore.dll")]
		private static extern int GetDpiForMonitor(
			IntPtr hmonitor,
			MONITOR_DPI_TYPE dpiType,
			out uint dpiX,
			out uint dpiY);

		private enum MONITOR_DPI_TYPE
		{
			MDT_Effective_DPI = 0,
			MDT_Angular_DPI = 1,
			MDT_Raw_DPI = 2,
			MDT_Default = MDT_Effective_DPI
		}

		private const int S_OK = 0x0;

		#endregion

		public const int DefaultDpi = 96;

		public static float GetNotificationAreaDpiScale()
		{
			return TryGetNotificationAreaDpi(out int dpi)
				? (float)dpi / DefaultDpi
				: 1F;
		}

		public static bool TryGetNotificationAreaDpi(out int dpi)
		{
			var taskbarHandle = FindWindowEx(
				IntPtr.Zero,
				IntPtr.Zero,
				"Shell_TrayWnd",
				string.Empty);
			if (taskbarHandle != IntPtr.Zero)
			{
				var notificationAreaHandle = FindWindowEx(
					taskbarHandle,
					IntPtr.Zero,
					"TrayNotifyWnd",
					string.Empty);
				if (notificationAreaHandle != IntPtr.Zero)
				{
					var monitorHandle = MonitorFromWindow(
						notificationAreaHandle,
						MONITOR_DEFAULTTO.MONITOR_DEFAULTTOPRIMARY);
					if (monitorHandle != IntPtr.Zero)
					{
						if (GetDpiForMonitor(
							monitorHandle,
							MONITOR_DPI_TYPE.MDT_Default,
							out uint dpiX,
							out _) == S_OK)
						{
							dpi = (int)dpiX;
							return true;
						}
					}
				}
			}
			dpi = default;
			return false;
		}
	}
}