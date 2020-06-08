using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HelloSwitcher
{
	internal class NotifyIconHelper
	{
		public static bool TryGetNotifyIconWindow(NotifyIcon notifyIcon, out NativeWindow window) =>
			TryGetNonPublicFieldValue(notifyIcon, "window", out window);

		private static bool TryGetNonPublicFieldValue<TInstance, TValue>(TInstance instance, string fieldName, out TValue fieldValue)
		{
			var fieldInfo = typeof(TInstance).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
			if (fieldInfo?.GetValue(instance) is TValue value)
			{
				fieldValue = value;
				return true;
			}
			fieldValue = default;
			return false;
		}
	}
}