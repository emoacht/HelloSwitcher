using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

namespace HelloSwitcher
{
	internal static class WindowHelper
	{
		#region Win32

		[DllImport("User32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool GetWindowPlacement(
			IntPtr hWnd,
			out WINDOWPLACEMENT lpwndpl);

		[DllImport("User32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool SetWindowPlacement(
			IntPtr hWnd,
			[In] ref WINDOWPLACEMENT lpwndpl);

		[StructLayout(LayoutKind.Sequential)]
		private struct WINDOWPLACEMENT
		{
			public uint length;
			public uint flags;
			public uint showCmd;
			public POINT ptMinPosition;
			public POINT ptMaxPosition;
			public RECT rcNormalPosition;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct POINT
		{
			public int x;
			public int y;

			public static implicit operator Point(POINT point) => new Point(point.x, point.y);
			public static implicit operator POINT(Point point) => new POINT { x = (int)point.X, y = (int)point.Y };
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct RECT
		{
			public int left;
			public int top;
			public int right;
			public int bottom;

			public RECT(int left, int top, int right, int bottom)
			{
				this.left = left;
				this.top = top;
				this.right = right;
				this.bottom = bottom;
			}

			public static implicit operator RECT(Rect rect) => new RECT((int)rect.Left, (int)rect.Top, (int)rect.Right, (int)rect.Bottom);
			public static implicit operator Rect(RECT rect) => new Rect(rect.left, rect.top, (rect.right - rect.left), (rect.bottom - rect.top));
		}

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

		public static bool TryGetScreenLocation(FrameworkElement element, out Rect location)
		{
			if (element is null)
				throw new ArgumentNullException(nameof(element));

			if (PresentationSource.FromVisual(element) is not null)
			{
				location = new Rect(
					element.PointToScreen(new Point(0, 0)),
					element.PointToScreen(new Point(element.ActualWidth, element.ActualHeight)));

				return true;
			}
			location = default;
			return false;
		}

		public static void SetWindowLocation(Window window, Rect location)
		{
			if ((location.Width <= 0) || (location.Height <= 0))
				throw new ArgumentException(nameof(location));

			var windowHandle = new WindowInteropHelper(window).Handle;
			if (!GetWindowPlacement(windowHandle, out WINDOWPLACEMENT windowPlacement))
				return;

			if (windowPlacement.rcNormalPosition == location)
				return;

			windowPlacement.rcNormalPosition = location;
			SetWindowPlacement(windowHandle, ref windowPlacement);
		}

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