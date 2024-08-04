using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HelloSwitcher.ViewModels;

public abstract class ViewModelBase : INotifyPropertyChanged, IDisposable
{
	#region INotifyPropertyChanged

	public event PropertyChangedEventHandler PropertyChanged;

	protected virtual bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
	{
		if (EqualityComparer<T>.Default.Equals(storage, value))
			return false;

		storage = value;
		RaisePropertyChanged(propertyName);
		return true;
	}

	protected virtual void RaisePropertyChanged([CallerMemberName] string propertyName = null) =>
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

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
		}

		// Free any unmanaged objects here.
		_isDisposed = true;
	}

	#endregion
}