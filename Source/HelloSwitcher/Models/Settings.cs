using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HelloSwitcher.Models
{
	public class Settings
	{
		public string BuiltInCameraDeviceInstanceId { get; set; }

		public string RemovableCameraDeviceInstanceId
		{
			get => _removableCameraDeviceInstanceId;
			set
			{
				_removableCameraDeviceInstanceId = value;
				RemovableCameraVidPid = new VidPid(value);
			}
		}
		private string _removableCameraDeviceInstanceId;

		internal VidPid RemovableCameraVidPid { get; private set; }

		public Guid RemovableCameraClassGuid { get; set; }

		internal bool IsLoaded { get; private set; }

		internal bool IsFilled => !string.IsNullOrWhiteSpace(BuiltInCameraDeviceInstanceId)
							   && !string.IsNullOrWhiteSpace(RemovableCameraDeviceInstanceId)
							   && (RemovableCameraClassGuid != default);

		#region Load/Save

		private static string FolderPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), nameof(HelloSwitcher));
		private static string FilePath => Path.Combine(FolderPath, "settings.txt");

		private const char Separator = '=';

		private static IEnumerable<PropertyDescriptor> EnumerateProperties() =>
			TypeDescriptor.GetProperties(typeof(Settings)) // This returns public properties only.
				.Cast<PropertyDescriptor>()
				.Where(x => !x.IsReadOnly);

		public async Task LoadAsync()
		{
			if (!File.Exists(FilePath))
				return;

			var properties = EnumerateProperties().ToList();

			var lines = await Task.Run(() => File.ReadAllLines(FilePath));

			foreach (var (key, value) in lines
				.Where(x => !string.IsNullOrEmpty(x))
				.Select(x => x.Split(new[] { Separator }, 2).Select(x => x.Trim()).ToArray())
				.Where(x => x.Length == 2)
				.Select(x => (key: x[0], value: x[1])))
			{
				int index = properties.FindIndex(x => x.Name == key);
				if (index < 0)
					continue;

				var property = properties[index];
				try
				{
					property.SetValue(this, property.Converter.ConvertFromString(value));
					properties.RemoveAt(index);
				}
				catch
				{
				}
			}

			IsLoaded = !properties.Any();
		}

		public async Task SaveAsync()
		{
			if (!Directory.Exists(FolderPath))
				Directory.CreateDirectory(FolderPath);

			var lines = EnumerateProperties().Select(x => $"{x.Name}{Separator}{x.GetValue(this)}");

			await Task.Run(() => File.WriteAllLines(FilePath, lines));
		}

		#endregion
	}
}