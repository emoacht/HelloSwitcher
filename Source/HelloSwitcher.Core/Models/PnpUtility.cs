using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace HelloSwitcher.Models;

/// <summary>
/// Utility methods by PnPUtil
/// </summary>
/// <remarks>
/// https://docs.microsoft.com/en-us/windows-hardware/drivers/devtest/pnputil
/// </remarks>
public static class PnpUtility
{
	public static Task<string[]> EnableAsync(string deviceInstanceId, CancellationToken cancellationToken = default) => ExecuteAsync($@"/enable-device ""{deviceInstanceId}""", cancellationToken);
	public static Task<string[]> DisableAsync(string deviceInstanceId, CancellationToken cancellationToken = default) => ExecuteAsync($@"/disable-device ""{deviceInstanceId}""", cancellationToken);

	private static async Task<string[]> ExecuteAsync(string arguments, CancellationToken cancellationToken)
	{
		var lines = await ExecuteDirectAsync(arguments, cancellationToken);
#if DEBUG
		foreach (var line in lines)
			Debug.WriteLine(line);
#endif
		return lines;
	}

	#region Type

	public class CameraItem
	{
		internal string ClassName { get; }
		public Guid ClassGuid { get; }

		public string DeviceInstanceId { get; }
		public string Description { get; }
		public string Manufacturer { get; }
		public string Status { get; }

		public CameraItem(IEnumerable<string> lines)
		{
			foreach (var (key, value, index) in lines
				.Where(x => !string.IsNullOrEmpty(x))
				.Select(x => x.Split(new[] { ':' }, 2).Select(x => x.Trim()).ToArray())
				.Where(x => x.Length == 2)
				.Select((x, index) => (key: x[0], value: x[1], index)))
			{
				switch (key)
				{
					case "Instance ID": DeviceInstanceId = value; continue;
					case "Device Description": Description = value; continue;
					case "Class Name": ClassName = value; continue;
					case "Class GUID": ClassGuid = Parse(value); continue;
					case "Manufacturer Name": Manufacturer = value; continue;
					case "Status": Status = value; continue;
				}

				// If ths OS's culture is a specific cultures (de-DE, fr-FR, es-ES, it-IT ...),
				// PnPUtil will produce language-specific outputs rather than English ones
				// even if the code page is set to en-US. In such case, rely on index number.
				switch (index)
				{
					case 0: DeviceInstanceId = value; break;
					case 1: Description = value; break;
					case 2: ClassName = value; break;
					case 3: ClassGuid = Parse(value); break;
					case 4: Manufacturer = value; break;
					case 5: Status = value; break;
				}
			}

			static Guid Parse(string source)
			{
				if (source is not null)
				{
					try
					{
						return new Guid(source);
					}
					catch (FormatException)
					{
					}
				}
				return default;
			}
		}
	}

	#endregion

	public static async Task<CameraItem[]> GetCamerasAsync(CancellationToken cancellationToken = default)
	{
		return [.. await GetCamerasAsync("Camera", cancellationToken),
				.. await GetCamerasAsync("Image", cancellationToken)];
	}

	public static async Task<CameraItem[]> GetCamerasAsync(string className, CancellationToken cancellationToken = default)
	{
		var lines = await ExecuteCommandLineAsync($"/enum-devices /class {className} /connected", cancellationToken);
#if DEBUG
		foreach (var line in lines)
			Debug.WriteLine(line);
#endif
		IEnumerable<CameraItem> Enumerate()
		{
			var buffer = new List<string>();

			foreach (var line in lines)
			{
				if (!string.IsNullOrEmpty(line))
				{
					buffer.Add(line);
				}
				else if (buffer.Count > 0)
				{
					if (buffer.Count >= 6)
					{
						yield return new CameraItem(buffer);
					}
					buffer.Clear();
				}
			}
		}

		return Enumerate().ToArray();
	}

	private static async Task<string[]> ExecuteDirectAsync(string arguments, CancellationToken cancellationToken)
	{
		using var canceller = new RedirectionCanceller();

		using var proc = new Process
		{
			StartInfo =
			{
				FileName = Path.Combine(Environment.SystemDirectory, "pnputil.exe"),
				Arguments = arguments,
				CreateNoWindow = true,
				UseShellExecute = false,
				RedirectStandardInput = false,
				RedirectStandardOutput = true
			},
			EnableRaisingEvents = true
		};

		var outputLines = new List<string>();

		var tcs = new TaskCompletionSource<bool>();

		void received(object sender, DataReceivedEventArgs e) => outputLines.Add(e.Data);
		void exited(object sender, EventArgs e) => tcs.SetResult(true);

		using var registration = cancellationToken.Register(() => tcs.TrySetCanceled());

		try
		{
			proc.OutputDataReceived += received;
			proc.Exited += exited;

			proc.Start();
			proc.BeginOutputReadLine();

			await tcs.Task;

			return outputLines.ToArray();
		}
		finally
		{
			proc.OutputDataReceived -= received;
			proc.Exited -= exited;
		}
	}

	private static async Task<string[]> ExecuteCommandLineAsync(string arguments, CancellationToken cancellationToken)
	{
		using var canceller = new RedirectionCanceller();

		using var proc = new Process
		{
			StartInfo =
			{
				FileName = Environment.GetEnvironmentVariable("ComSpec"),
				CreateNoWindow = true,
				UseShellExecute = false,
				RedirectStandardInput = true,
				RedirectStandardOutput = true
			},
			EnableRaisingEvents = true
		};

		string[] inputLines =
		{
			"chcp 437", // Change code page to 437 (US English).
			$"pnputil {arguments}",
			"exit"
		};

		var outputLines = new List<string>();

		var tcs = new TaskCompletionSource<bool>();

		void received(object sender, DataReceivedEventArgs e) => outputLines.Add(e.Data);
		void exited(object sender, EventArgs e) => tcs.SetResult(true);

		using var registration = cancellationToken.Register(() => tcs.TrySetCanceled());

		try
		{
			proc.OutputDataReceived += received;
			proc.Exited += exited;

			proc.Start();
			proc.BeginOutputReadLine();

			using (var writer = proc.StandardInput)
			{
				if (writer.BaseStream.CanWrite)
				{
					foreach (var inputLine in inputLines)
						writer.WriteLine(inputLine);
				}
			}

			await tcs.Task;

			return outputLines.ToArray();
		}
		finally
		{
			proc.OutputDataReceived -= received;
			proc.Exited -= exited;
		}
	}

	private class RedirectionCanceller : IDisposable
	{
		[DllImport("Kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool Wow64DisableWow64FsRedirection(ref IntPtr OldValue);

		[DllImport("Kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool Wow64RevertWow64FsRedirection(IntPtr OldValue);

		private readonly IntPtr _pointer;

		public RedirectionCanceller()
		{
			if (!Environment.Is64BitProcess)
				Wow64DisableWow64FsRedirection(ref _pointer);
		}

		public void Dispose()
		{
			if (!Environment.Is64BitProcess)
				Wow64RevertWow64FsRedirection(_pointer);
		}
	}
}