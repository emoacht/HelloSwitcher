using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;

using HelloSwitcher.Models;

namespace HelloSwitcher.ViewModels;

public class SettingsWindowViewModel : ViewModelBase
{
	public ObservableCollection<CameraViewModel> Cameras { get; } = [];
	public ListCollectionView CamerasView { get; }

	public string OperationStatus
	{
		get => _operationStatus;
		private set => SetProperty(ref _operationStatus, value);
	}
	private string _operationStatus;

	public bool CanApply
	{
		get => _canApply;
		private set => SetProperty(ref _canApply, value);
	}
	private bool _canApply;

	public bool RunAsService
	{
		get => _runAsService;
		set => SetProperty(ref _runAsService, value);
	}
	private bool _runAsService;

	private readonly App _app;
	private Settings Settings => _app.Settings;

	public SettingsWindowViewModel(App app)
	{
		this._app = app ?? throw new ArgumentNullException(nameof(app));

		CamerasView = new ListCollectionView(Cameras);
		CamerasView.SortDescriptions.Add(new SortDescription(nameof(CameraViewModel.IsRemovable), ListSortDirection.Ascending));
		CamerasView.SortDescriptions.Add(new SortDescription(nameof(CameraViewModel.SortIndex), ListSortDirection.Ascending));
		CamerasView.IsLiveSorting = true;

		Cameras.CollectionChanged += (_, _) => OnChanged();
		CameraViewModel.IsBuiltInCameraSelectionChanged += (_, _) => OnChanged();
		CameraViewModel.IsRemovableCameraSelectionChanged += (_, _) => OnChanged();

		void OnChanged()
		{
			CanApply = (Cameras.Any(x => x.IsBuiltInCameraSelected) || Settings.IsBuiltInCameraFilled)
					&& (Cameras.Any(x => x.IsRemovableCameraSelected) || Settings.IsRemovableCameraFilled);
		}

		this.RunAsService = _app.RunAsService;
	}

	public async Task SearchAsync()
	{
		var pnpCameras = await PnpUtility.GetCamerasAsync();
		var usbCameras = DeviceUsbHelper.EnumerateUsbCameras().ToArray();

		// Add and remove cameras.
		var oldCameras = Cameras.ToList();
		int sortIndex = 0;

		foreach (var pnpCamera in pnpCameras)
		{
			var usbCamera = usbCameras.FirstOrDefault(x => string.Equals(pnpCamera.DeviceInstanceId, x.DeviceInstanceId, StringComparison.OrdinalIgnoreCase));
			var isRemovable = (usbCamera?.IsRemovable is true);

			int index = oldCameras.FindIndex(x => string.Equals(x.DeviceInstanceId, pnpCamera.DeviceInstanceId, StringComparison.Ordinal));
			if (index >= 0)
			{
				oldCameras[index].Status = pnpCamera.Status;
				oldCameras[index].IsRemovable = isRemovable;
				oldCameras[index].SortIndex = sortIndex++;
				oldCameras.RemoveAt(index);
			}
			else
			{
				Cameras.Add(new CameraViewModel(pnpCamera.ClassGuid, pnpCamera.DeviceInstanceId, pnpCamera.Description, pnpCamera.Manufacturer, pnpCamera.Status, isRemovable, sortIndex++));
			}
		}

		foreach (var oldCamera in oldCameras)
		{
			oldCamera.Dispose();
			Cameras.Remove(oldCamera);
		}

		// Set selected cameras.
		var builtInCamera = Cameras.FirstOrDefault(x => string.Equals(x.DeviceInstanceId, Settings.BuiltInCameraDeviceInstanceId, StringComparison.Ordinal));
		if (builtInCamera is not null)
			builtInCamera.IsBuiltInCameraSelected = true;

		var removableCamera = Cameras.FirstOrDefault(x => string.Equals(x.DeviceInstanceId, Settings.RemovableCameraDeviceInstanceId, StringComparison.Ordinal));
		if (removableCamera is not null)
			removableCamera.IsRemovableCameraSelected = true;

		// Update operation status.
		string GetStatus()
		{
			if (!Settings.IsFilled)
				return "Settings is required";

			if (builtInCamera is null)
				return "Specified built-in camera is not found";

			if (removableCamera is null)
				return "Specified USB camera is disconnected";
			else
				return "Specified USB camera is connected";
		}

		OperationStatus = GetStatus();
	}

	public async Task ApplyAsync()
	{
		var isUpdated = false;

		var builtInCamera = Cameras.FirstOrDefault(x => x.IsBuiltInCameraSelected);
		if ((builtInCamera is not null) &&
			(Settings.BuiltInCameraDeviceInstanceId != builtInCamera.DeviceInstanceId))
		{
			Settings.BuiltInCameraDeviceInstanceId = builtInCamera.DeviceInstanceId;
			isUpdated = true;
		}

		var removableCamera = Cameras.FirstOrDefault(x => x.IsRemovableCameraSelected);
		if ((removableCamera is not null) &&
			(Settings.RemovableCameraDeviceInstanceId != removableCamera.DeviceInstanceId))
		{
			Settings.RemovableCameraDeviceInstanceId = removableCamera.DeviceInstanceId;
			Settings.RemovableCameraClassGuid = removableCamera.ClassGuid;
			isUpdated = true;
		}

		if (isUpdated)
			await Settings.SaveAsync();

		_app.RunAsService = this.RunAsService;
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
			foreach (var camera in Cameras)
				camera.Dispose();
		}

		// Free any unmanaged objects here.
		_isDisposed = true;

		base.Dispose(disposing);
	}

	#endregion
}