﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

using HelloSwitcher.Models;

namespace HelloSwitcher.ViewModels
{
	public class SettingsWindowViewModel : ViewModelBase
	{
		public ObservableCollection<CameraViewModel> Cameras { get; } = new();
		public ListCollectionView CamerasView { get; }

		public string OperationStatus
		{
			get => _operationStatus;
			private set => SetPropertyValue(ref _operationStatus, value);
		}
		private string _operationStatus;

		public bool CanApply
		{
			get => _canApply;
			private set => SetPropertyValue(ref _canApply, value);
		}
		private bool _canApply;

		private readonly Settings _settings;

		public SettingsWindowViewModel(Settings settings)
		{
			this._settings = settings ?? throw new ArgumentNullException(nameof(settings));

			CamerasView = new ListCollectionView(Cameras);
			CamerasView.SortDescriptions.Add(new SortDescription(nameof(CameraViewModel.IsRemovable), ListSortDirection.Ascending));
			CamerasView.SortDescriptions.Add(new SortDescription(nameof(CameraViewModel.SortIndex), ListSortDirection.Ascending));
			CamerasView.IsLiveSorting = true;

			Cameras.CollectionChanged += (_, _) => OnChanged();
			CameraViewModel.IsBuiltInCameraSelectionChanged += (_, _) => OnChanged();
			CameraViewModel.IsRemovableCameraSelectionChanged += (_, _) => OnChanged();

			void OnChanged()
			{
				CanApply = Cameras.Any(x => x.IsBuiltInCameraSelected)
						&& Cameras.Any(x => x.IsRemovableCameraSelected);
			}
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
			var builtInCamera = Cameras.FirstOrDefault(x => string.Equals(x.DeviceInstanceId, _settings.BuiltInCameraDeviceInstanceId, StringComparison.Ordinal));
			if (builtInCamera is not null)
				builtInCamera.IsBuiltInCameraSelected = true;

			var removableCamera = Cameras.FirstOrDefault(x => string.Equals(x.DeviceInstanceId, _settings.RemovableCameraDeviceInstanceId, StringComparison.Ordinal));
			if (removableCamera is not null)
				removableCamera.IsRemovableCameraSelected = true;

			// Update operation status.
			string GetStatus()
			{
				if (!_settings.IsFilled)
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
			var builtInCamera = Cameras.FirstOrDefault(x => x.IsBuiltInCameraSelected);
			if (builtInCamera is not null)
				_settings.BuiltInCameraDeviceInstanceId = builtInCamera.DeviceInstanceId;

			var removableCamera = Cameras.FirstOrDefault(x => x.IsRemovableCameraSelected);
			if (removableCamera is not null)
			{
				_settings.RemovableCameraDeviceInstanceId = removableCamera.DeviceInstanceId;
				_settings.RemovableCameraClassGuid = removableCamera.ClassGuid;
			}

			await _settings.SaveAsync();
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
}