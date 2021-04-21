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
	/// Utility methods for USB devices by Device Information Functions
	/// </summary>
	internal static class DeviceUsbHelper
	{
		#region Win32

		[DllImport("SetupAPI.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr SetupDiGetClassDevs(
			[MarshalAs(UnmanagedType.LPStruct), In] Guid ClassGuid,
			[MarshalAs(UnmanagedType.LPWStr), In] string Enumerator,
			IntPtr hwndParent,
			DIGCF Flags);

		[Flags]
		private enum DIGCF : uint
		{
			DIGCF_DEFAULT = 0x00000001,
			DIGCF_PRESENT = 0x00000002,

			/// <summary>
			/// Return a list of installed devices for all device setup classes or all device interface classes. 
			/// </summary>
			/// <remarks>
			/// This flag overwrites ClassGuid parameter.
			/// https://docs.microsoft.com/en-us/windows/win32/api/setupapi/nf-setupapi-setupdigetclassdevsw#remarks
			/// </remarks>
			DIGCF_ALLCLASSES = 0x00000004,

			DIGCF_PROFILE = 0x00000008,
			DIGCF_DEVICEINTERFACE = 0x00000010,
		}

		[DllImport("SetupAPI.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool SetupDiDestroyDeviceInfoList(IntPtr DeviceInfoSet);

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

		[DllImport("SetupAPI.dll", CharSet = CharSet.Auto, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool SetupDiGetDeviceRegistryProperty(
			IntPtr DeviceInfoSet,
			ref SP_DEVINFO_DATA DeviceInfoData,
			SPDRP Property,
			IntPtr PropertyRegDataType,
			IntPtr PropertyBuffer,
			uint PropertyBufferSize,
			out uint RequiredSize);

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

		[Flags]
		private enum CM_DEVCAP : uint
		{
			CM_DEVCAP_LOCKSUPPORTED = 0x00000001,
			CM_DEVCAP_EJECTSUPPORTED = 0x00000002,
			CM_DEVCAP_REMOVABLE = 0x00000004,
			CM_DEVCAP_DOCKDEVICE = 0x00000008,
			CM_DEVCAP_UNIQUEID = 0x00000010,
			CM_DEVCAP_SILENTINSTALL = 0x00000020,
			CM_DEVCAP_RAWDEVICEOK = 0x00000040,
			CM_DEVCAP_SURPRISEREMOVALOK = 0x00000080,
			CM_DEVCAP_HARDWAREDISABLED = 0x00000100,
			CM_DEVCAP_NONDYNAMIC = 0x00000200
		}

		[DllImport("Setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool SetupDiGetDeviceInstanceId(
			IntPtr DeviceInfoSet,
			ref SP_DEVINFO_DATA DeviceInfoData,
			[Out] StringBuilder DeviceInstanceId,
			uint DeviceInstanceIdSize,
			out uint RequiredSize);

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
		private const int ERROR_NO_MORE_ITEMS = 259;

		#endregion

		public const int WM_DEVICECHANGE = 0x0219;

		public const int DBT_DEVICEARRIVAL = 0x8000;
		public const int DBT_DEVICEREMOVECOMPLETE = 0x8004;

		#region Type

		public class UsbCameraItem
		{
			internal Guid ClassGuid { get; }

			public string DeviceInstanceId { get; }
			public string FriendlyName { get; }
			public bool IsRemovable { get; }

			public UsbCameraItem(Guid classGuid, string deviceInstanceId, string friendlyName, bool isRemovable)
			{
				this.ClassGuid = classGuid;
				this.DeviceInstanceId = deviceInstanceId;
				this.FriendlyName = friendlyName;
				this.IsRemovable = isRemovable;
			}
		}

		#endregion

		#region Check

		private readonly static Guid CameraClassGuid = new Guid("{ca3e7ab9-b4c3-4ae6-8251-579ef933890f}");
		private readonly static Guid ImageClassGuid = new Guid("{6bdd1fc6-810f-11d0-bec7-08002be2092f}");

		public static IEnumerable<UsbCameraItem> EnumerateUsbCameras()
		{
			static UsbCameraItem Convert(Guid classGuid, IntPtr deviceInfoSet, SP_DEVINFO_DATA deviceInfoData)
			{
				var friendlyName = GetDevicePropertyString(deviceInfoSet, deviceInfoData, SPDRP.SPDRP_FRIENDLYNAME);
				var capabilities = (CM_DEVCAP)GetDevicePropertyUInt(deviceInfoSet, deviceInfoData, SPDRP.SPDRP_CAPABILITIES);
				var isRemovable = capabilities.HasFlag(CM_DEVCAP.CM_DEVCAP_REMOVABLE);
				var deviceInstanceId = GetDeviceInstanceId(deviceInfoSet, deviceInfoData);

				return new UsbCameraItem(classGuid, deviceInstanceId, friendlyName, isRemovable);
			}

			return EnumerateUsbDevices(CameraClassGuid, Convert)
				.Concat(EnumerateUsbDevices(ImageClassGuid, Convert));
		}

		public static bool UsbCameraExists(Guid classGuid, string deviceInstanceId)
		{
			return EnumerateUsbDevices(classGuid, (classGuid, deviceInfoSet, deviceInfoData) => GetDeviceInstanceId(deviceInfoSet, deviceInfoData))
				.Any(x => string.Equals(x, deviceInstanceId, StringComparison.OrdinalIgnoreCase));
		}

		private static IEnumerable<T> EnumerateUsbDevices<T>(Guid classGuid, Func<Guid, IntPtr, SP_DEVINFO_DATA, T> convert)
		{
			var deviceInfoSet = IntPtr.Zero;
			try
			{
				deviceInfoSet = SetupDiGetClassDevs(
					classGuid,
					"USB",
					IntPtr.Zero,
					DIGCF.DIGCF_PRESENT);
				if ((Environment.Is64BitProcess ? deviceInfoSet.ToInt64() : deviceInfoSet.ToInt32()) == INVALID_HANDLE_VALUE)
					yield break;

				uint index = 0;

				while (true)
				{
					var deviceInfoData = new SP_DEVINFO_DATA { cbSize = (uint)Marshal.SizeOf<SP_DEVINFO_DATA>() };

					if (SetupDiEnumDeviceInfo(
						deviceInfoSet,
						index,
						ref deviceInfoData))
					{
						// var classGuidString = GetDevicePropertyString(deviceInfoSet, deviceInfoData, SPDRP.SPDRP_CLASSGUID);
						yield return convert.Invoke(classGuid, deviceInfoSet, deviceInfoData);
					}
					else if (Marshal.GetLastWin32Error() == ERROR_NO_MORE_ITEMS)
					{
						yield break;
					}
					index++;
				}
			}
			finally
			{
				if (deviceInfoSet != IntPtr.Zero)
					SetupDiDestroyDeviceInfoList(deviceInfoSet);
			}
		}

		private static string GetDevicePropertyString(IntPtr DeviceInfoSet, SP_DEVINFO_DATA DeviceInfoData, SPDRP property)
		{
			return GetDevicePropertyValue(DeviceInfoSet, DeviceInfoData, property, (pointer, _) => Marshal.PtrToStringAuto(pointer));
		}

		private static uint GetDevicePropertyUInt(IntPtr DeviceInfoSet, SP_DEVINFO_DATA DeviceInfoData, SPDRP property)
		{
			return GetDevicePropertyValue(DeviceInfoSet, DeviceInfoData, property, (pointer, size) =>
			{
				var array = new byte[size];
				Marshal.Copy(pointer, array, 0, (int)size);
				return BitConverter.ToUInt32(array, 0);
			});
		}

		private static T GetDevicePropertyValue<T>(IntPtr DeviceInfoSet, SP_DEVINFO_DATA DeviceInfoData, SPDRP property, Func<IntPtr, uint, T> convert)
		{
			SetupDiGetDeviceRegistryProperty(
				DeviceInfoSet,
				ref DeviceInfoData,
				property,
				IntPtr.Zero,
				IntPtr.Zero,
				0,
				out uint requiredSize);

			var buffer = IntPtr.Zero;
			try
			{
				buffer = Marshal.AllocHGlobal((int)requiredSize);

				if (SetupDiGetDeviceRegistryProperty(
					DeviceInfoSet,
					ref DeviceInfoData,
					property,
					IntPtr.Zero,
					buffer,
					requiredSize,
					out _))
				{
					return convert.Invoke(buffer, requiredSize);
				}
				return default;
			}
			finally
			{
				if (buffer != IntPtr.Zero)
					Marshal.FreeHGlobal(buffer);
			}
		}

		private static string GetDeviceInstanceId(IntPtr DeviceInfoSet, SP_DEVINFO_DATA DeviceInfoData)
		{
			SetupDiGetDeviceInstanceId(
				DeviceInfoSet,
				ref DeviceInfoData,
				null,
				0,
				out uint requiredSize);

			var buffer = new StringBuilder((int)requiredSize);

			if (SetupDiGetDeviceInstanceId(
				DeviceInfoSet,
				ref DeviceInfoData,
				buffer,
				requiredSize,
				out _))
			{
				return buffer.ToString();
			}
			return default;
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