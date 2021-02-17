using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HelloSwitcher.ViewModels
{
	public class CameraViewModel : ViewModelBase
	{
		internal Guid ClassGuid { get; }

		public string DeviceInstanceId { get; }
		public string Description { get; }
		public string Manufacturer { get; }

		public string Status
		{
			get => _status;
			set => SetPropertyValue(ref _status, value);
		}
		private string _status;

		public bool IsRemovable
		{
			get => _isRemovable;
			set => SetPropertyValue(ref _isRemovable, value);
		}
		private bool _isRemovable;

		public int SortIndex { get; set; }

		#region Selection

		public bool IsBuiltInCameraSelected
		{
			get => _isBuiltInCameraSelected;
			set
			{
				if (SetPropertyValue(ref _isBuiltInCameraSelected, value))
				{
					if (value)
						IsRemovableCameraSelected = false;

					IsBuiltInCameraSelectionChanged?.Invoke(this, value);
				}
			}
		}
		private bool _isBuiltInCameraSelected;

		public bool IsRemovableCameraSelected
		{
			get => _isRemovableCameraSelected;
			set
			{
				if (SetPropertyValue(ref _isRemovableCameraSelected, value))
				{
					if (value)
						IsBuiltInCameraSelected = false;

					IsRemovableCameraSelectionChanged?.Invoke(this, value);
				}
			}
		}
		private bool _isRemovableCameraSelected;

		internal static event EventHandler<bool> IsBuiltInCameraSelectionChanged; // Static
		internal static event EventHandler<bool> IsRemovableCameraSelectionChanged; // Static

		private void OnIsBuiltInCameraSelectionChanged(object sender, bool isSelected)
		{
			if ((sender != this) && isSelected)
				IsBuiltInCameraSelected = false;
		}

		private void OnIsRemovableCameraSelectionChanged(object sender, bool isSelected)
		{
			if ((sender != this) && isSelected)
				IsRemovableCameraSelected = false;
		}

		#endregion

		public CameraViewModel(Guid classGuid, string devideInstanceId, string description, string manufacturer, string status, bool isRemovable, int sortIndex)
		{
			this.ClassGuid = classGuid;
			this.DeviceInstanceId = devideInstanceId;
			this.Description = description;
			this.Manufacturer = manufacturer;
			this.Status = status;
			this.IsRemovable = isRemovable;
			this.SortIndex = sortIndex;

			IsBuiltInCameraSelectionChanged += OnIsBuiltInCameraSelectionChanged;
			IsRemovableCameraSelectionChanged += OnIsRemovableCameraSelectionChanged;
		}

		#region IDisposable

		private bool _isDisposed = false;

		protected override void Dispose(bool disposing)
		{
			if (_isDisposed)
				return;

			if (disposing)
			{
				// Free any other managed objects here.
				IsBuiltInCameraSelectionChanged -= OnIsBuiltInCameraSelectionChanged;
				IsRemovableCameraSelectionChanged -= OnIsRemovableCameraSelectionChanged;
			}

			// Free any unmanaged objects here.
			_isDisposed = true;

			base.Dispose(disposing);
		}

		#endregion
	}
}