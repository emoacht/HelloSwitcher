using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace HelloSwitcher.Models
{
	/// <summary>
	/// Utility methods for USB devices by Setup API
	/// </summary>
	internal static class SetupUsbHelper
	{
		#region Win32

		[DllImport("SetupAPI.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr SetupDiGetClassDevs(
			IntPtr ClassGuid,
			string Enumerator,
			IntPtr hwndParent,
			DIGCF Flags);

		[Flags]
		private enum DIGCF : uint
		{
			DIGCF_DEFAULT = 0x00000001,
			DIGCF_PRESENT = 0x00000002,
			DIGCF_ALLCLASSES = 0x00000004,
			DIGCF_PROFILE = 0x00000008,
			DIGCF_DEVICEINTERFACE = 0x00000010,
		}

		[DllImport("SetupAPI.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool SetupDiEnumDeviceInfo(
			IntPtr DeviceInfoSet,
			uint MemberIndex,
			ref SP_DEVINFO_DATA DeviceInfoData);

		[StructLayout(LayoutKind.Sequential)]
		private struct SP_DEVINFO_DATA
		{
			public uint cbSize;
			public Guid ClassGuid;
			public uint DevInst;
			public IntPtr Reserved;
		}

		[DllImport("SetupAPI.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool SetupDiDestroyDeviceInfoList(IntPtr DeviceInfoSet);

		[DllImport("SetupAPI.dll", CharSet = CharSet.Auto, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool SetupDiGetDeviceRegistryProperty(
			IntPtr DeviceInfoSet,
			ref SP_DEVINFO_DATA DeviceInfoData,
			SPDRP Property,
			IntPtr PropertyRegDataType,
			IntPtr PropertyBuffer,
			uint PropertyBufferSize,
			ref uint RequiredSize);

		private enum SPDRP : uint
		{
			SPDRP_DEVICEDESC = 0x00000000,
			SPDRP_HARDWAREID = 0x00000001,
			SPDRP_COMPATIBLEIDS = 0x00000002,
			SPDRP_SERVICE = 0x00000004,
			SPDRP_CLASS = 0x00000007,
			SPDRP_CLASSGUID = 0x00000008,
			SPDRP_DRIVER = 0x00000009,
			SPDRP_CONFIGFLAGS = 0x0000000A,
			SPDRP_MFG = 0x0000000B,
			SPDRP_FRIENDLYNAME = 0x0000000C,
			SPDRP_LOCATION_INFORMATION = 0x0000000D,
			SPDRP_PHYSICAL_DEVICE_OBJECT_NAME = 0x0000000E,
			SPDRP_CAPABILITIES = 0x0000000F,
			SPDRP_UI_NUMBER = 0x00000010,
			SPDRP_UPPERFILTERS = 0x00000011,
			SPDRP_LOWERFILTERS = 0x00000012,
			SPDRP_BUSTYPEGUID = 0x00000013,
			SPDRP_LEGACYBUSTYPE = 0x00000014,
			SPDRP_BUSNUMBER = 0x00000015,
			SPDRP_ENUMERATOR_NAME = 0x00000016,
			SPDRP_SECURITY = 0x00000017,
			SPDRP_SECURITY_SDS = 0x00000018,
			SPDRP_DEVTYPE = 0x00000019,
			SPDRP_EXCLUSIVE = 0x0000001A,
			SPDRP_CHARACTERISTICS = 0x0000001B,
			SPDRP_ADDRESS = 0x0000001C,
			SPDRP_UI_NUMBER_DESC_FORMAT = 0X0000001D,
			SPDRP_DEVICE_POWER_DATA = 0x0000001E,
			SPDRP_REMOVAL_POLICY = 0x0000001F,
			SPDRP_REMOVAL_POLICY_HW_DEFAULT = 0x00000020,
			SPDRP_REMOVAL_POLICY_OVERRIDE = 0x00000021,
			SPDRP_INSTALL_STATE = 0x00000022,
			SPDRP_LOCATION_PATHS = 0x00000023
		}

		[DllImport("User32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr RegisterDeviceNotification(
			IntPtr hRecipient,
			IntPtr NotificationFilter,
			uint Flags);

		[DllImport("User32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool UnregisterDeviceNotification(IntPtr Handle);

		[StructLayout(LayoutKind.Sequential)]
		private struct DEV_BROADCAST_HDR
		{
			public uint dbch_size;
			public uint dbch_devicetype;
			public uint dbch_reserved;
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		private struct DEV_BROADCAST_DEVICEINTERFACE
		{
			public uint dbcc_size;
			public uint dbcc_devicetype;
			public uint dbcc_reserved;
			public Guid dbcc_classguid;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
			public string dbcc_name;
		}

		private const int INVALID_HANDLE_VALUE = -1;

		#endregion

		public const int WM_DEVICECHANGE = 0x0219;

		public const int DBT_DEVICEARRIVAL = 0x8000;
		public const int DBT_DEVICEREMOVECOMPLETE = 0x8004;

		#region Check

		private const int BUFFER_SIZE = 1024;

		public static bool? UsbDeviceExists(string deviceId)
		{
			var deviceInfoSet = IntPtr.Zero;
			var propertyBuffer = IntPtr.Zero;
			try
			{
				propertyBuffer = Marshal.AllocHGlobal(BUFFER_SIZE);

				deviceInfoSet = SetupDiGetClassDevs(
					IntPtr.Zero,
					"USB",
					IntPtr.Zero,
					DIGCF.DIGCF_PRESENT | DIGCF.DIGCF_ALLCLASSES);
				if (deviceInfoSet.ToInt32() == INVALID_HANDLE_VALUE)
					return null;

				uint index = 0;
				while (true)
				{
					var deviceInfoData = new SP_DEVINFO_DATA { cbSize = (uint)Marshal.SizeOf<SP_DEVINFO_DATA>() };

					if (!SetupDiEnumDeviceInfo(
						deviceInfoSet,
						index,
						ref deviceInfoData))
					{
						break;
					}

					uint requiredSize = 0;

					// First attempt is to get required size.
					SetupDiGetDeviceRegistryProperty(
						deviceInfoSet,
						ref deviceInfoData,
						SPDRP.SPDRP_HARDWAREID,
						IntPtr.Zero,
						IntPtr.Zero,
						0,
						ref requiredSize);

					if (requiredSize <= BUFFER_SIZE)
					{
						// Second attempt is to get property value.
						if (SetupDiGetDeviceRegistryProperty(
							deviceInfoSet,
							ref deviceInfoData,
							SPDRP.SPDRP_HARDWAREID,
							IntPtr.Zero,
							propertyBuffer,
							BUFFER_SIZE,
							ref requiredSize) &&
							(propertyBuffer != IntPtr.Zero))
						{
							var hardwareId = Marshal.PtrToStringAuto(propertyBuffer);
							Debug.WriteLine($"ID: {hardwareId}");

							if (hardwareId?.StartsWith(deviceId, StringComparison.OrdinalIgnoreCase) == true)
								return true;
						}
					}
					index++;
				}
				return false;
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex);
				return null;
			}
			finally
			{
				if (deviceInfoSet != IntPtr.Zero)
					SetupDiDestroyDeviceInfoList(deviceInfoSet);

				if (propertyBuffer != IntPtr.Zero)
					Marshal.FreeHGlobal(propertyBuffer);
			}
		}

		#endregion

		#region Register/Unregister

		private const int DBT_DEVTYP_DEVICEINTERFACE = 0x00000005;
		private static readonly Guid GUID_DEVINTERFACE_USB_DEVICE = new Guid("A5DCBF10-6530-11D2-901F-00C04FB951ED");

		private static IntPtr _notificationHandle;

		public static bool RegisterUsbDeviceNotification(IntPtr windowHandle)
		{
			var dbcc = new DEV_BROADCAST_DEVICEINTERFACE
			{
				dbcc_size = (uint)Marshal.SizeOf<DEV_BROADCAST_DEVICEINTERFACE>(),
				dbcc_devicetype = DBT_DEVTYP_DEVICEINTERFACE,
				dbcc_classguid = GUID_DEVINTERFACE_USB_DEVICE,
			};

			var buffer = IntPtr.Zero;
			try
			{
				buffer = Marshal.AllocHGlobal((int)dbcc.dbcc_size);
				Marshal.StructureToPtr(dbcc, buffer, true);

				_notificationHandle = RegisterDeviceNotification(
					windowHandle,
					buffer,
					0);

				return (_notificationHandle != IntPtr.Zero);
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex);
				return false;
			}
			finally
			{
				if (buffer != IntPtr.Zero)
					Marshal.FreeHGlobal(buffer);
			}
		}

		public static void UnregisterUsbDeviceNotification()
		{
			UnregisterDeviceNotification(_notificationHandle);
		}

		public static bool TryGetDeviceName(IntPtr LParam, out string deviceName)
		{
			if (LParam != IntPtr.Zero)
			{
				var dbch = Marshal.PtrToStructure<DEV_BROADCAST_HDR>(LParam);
				if (dbch.dbch_devicetype == DBT_DEVTYP_DEVICEINTERFACE)
				{
					var dbcc = Marshal.PtrToStructure<DEV_BROADCAST_DEVICEINTERFACE>(LParam);
					deviceName = dbcc.dbcc_name;
					return true;
				}
			}
			deviceName = default;
			return false;
		}

		#endregion
	}
}