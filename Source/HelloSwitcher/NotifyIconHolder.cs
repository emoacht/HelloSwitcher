using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Threading;

namespace HelloSwitcher
{
	public class NotifyIconHolder : IDisposable
	{
		#region Type

		private class CustomColorTable : ProfessionalColorTable
		{
			public override System.Drawing.Color ImageMarginGradientBegin => System.Drawing.Color.Transparent;
			public override System.Drawing.Color ImageMarginGradientMiddle => System.Drawing.Color.Transparent;
			public override System.Drawing.Color ImageMarginGradientEnd => System.Drawing.Color.Transparent;
		}

		#endregion

		private readonly NotifyIcon _notifyIcon;

		public NotifyIconHolder(string[] iconUriStrings, string iconText, (ToolStripItemType type, string text, Action action)[] menus)
		{
			_icons = iconUriStrings.Select(x =>
				{
					var iconResource = System.Windows.Application.GetResourceStream(new Uri(x));
					if (iconResource is null)
						return default;

					using var iconStream = iconResource.Stream;
					return (icon: new System.Drawing.Icon(iconStream),
							text: iconText + Environment.NewLine + Path.GetFileNameWithoutExtension(x));
				})
				.Where(x => x.icon is not null)
				.ToArray();

			if (_icons.Count < 2)
				throw new ArgumentException("At least two valid URI strings are required.");

			_notifyIcon = new NotifyIcon();
			IconIndex = 0;

			_notifyIcon.ContextMenuStrip = new ContextMenuStrip { Renderer = new ToolStripProfessionalRenderer(new CustomColorTable()) };
			foreach (var (type, text, action, index) in menus.Select((x, index) => (x.type, x.text, x.action, index)))
			{
				var addedMargin = new Padding(left: 0, top: (index == 0 ? 4 : 0), right: 0, bottom: (index == menus.Length - 1 ? 4 : 0));

				switch (type)
				{
					case ToolStripItemType.Button:
						_notifyIcon.ContextMenuStrip.Items.Add(new ToolStripMenuItem(text, null, (_, _) => action?.Invoke())
						{
							Margin = addedMargin,
							Padding = new Padding(0, 0, 0, 4)
						});
						break;
					case ToolStripItemType.Label:
						_notifyIcon.ContextMenuStrip.Items.Add(new ToolStripLabel(text)
						{
							Margin = new Padding(0, 4, 0, 4) + addedMargin
						});
						break;
					case ToolStripItemType.Separator:
						_notifyIcon.ContextMenuStrip.Items.Add(new ToolStripSeparator
						{
							Margin = new Padding(0, 4, 0, 4)
						});
						break;
				}
			}

			// Show NotifyIcon.
			_notifyIcon.Visible = true;
		}

		#region Icons

		private readonly IReadOnlyList<(System.Drawing.Icon icon, string text)> _icons;

		internal void UpdateIcon(bool exists)
		{
			IconIndex = Convert.ToInt32(exists);
		}

		/// <summary>
		/// Index number of icons
		/// </summary>
		/// <remarks>
		/// 0: Disconnected icon
		/// 1: Connected icon
		/// </remarks>
		public int IconIndex
		{
			get => _iconIndex;
			set
			{
				if ((_iconIndex == value) || (value < 0) || (_icons.Count <= value))
					return;

				_iconIndex = value;
				_notifyIcon.Icon = _icons[value].icon;
				_notifyIcon.Text = _icons[value].text;
			}
		}
		private int _iconIndex = -1; // -1 is to let 0 come in as the first value.

		#endregion

		#region IDisposable

		private bool _isDisposed = false;

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (_isDisposed)
				return;

			if (disposing)
			{
				// Free any other managed objects here.
				_notifyIcon?.ContextMenuStrip?.Dispose();
				_notifyIcon?.Dispose();
			}

			// Free any unmanaged objects here.
			_isDisposed = true;
		}

		#endregion
	}

	public enum ToolStripItemType
	{
		Button = 0,
		Label,
		Separator
	}
}