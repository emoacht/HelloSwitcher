using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace HelloSwitcher;

public class NotifyIconHolder : IDisposable
{
	private readonly NotifyIcon _notifyIcon;

	public event EventHandler MouseRightClick;

	public NotifyIconHolder(string[] iconUriStrings, string iconText)
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

		_notifyIcon.MouseClick += (_, e) =>
		{
			if (e.Button == MouseButtons.Right)
				MouseRightClick?.Invoke(this, EventArgs.Empty);
		};

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