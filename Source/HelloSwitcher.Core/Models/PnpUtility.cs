﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace HelloSwitcher.Models
{
	/// <summary>
	/// Utility methods by PnPUtil
	/// </summary>
	/// <remarks>
	/// https://docs.microsoft.com/en-us/windows-hardware/drivers/devtest/pnputil
	/// </remarks>
	public static class PnpUtility
	{
		public static Task EnableAsync(string deviceInstanceId) => ExecuteAsync($@"/enable-device ""{deviceInstanceId}""");
		public static Task DisableAsync(string deviceInstanceId) => ExecuteAsync($@"/disable-device ""{deviceInstanceId}""");

		private static async Task ExecuteAsync(string arguments)
		{
			var lines = await ExecuteDirectAsync(arguments);
#if DEBUG
			foreach (var line in lines)
				Debug.WriteLine($"RE {line}");
#endif
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
			public bool IsEnabled { get; }

			public CameraItem(IEnumerable<string> lines)
			{
				foreach (var (key, value) in lines
					.Where(x => !string.IsNullOrEmpty(x))
					.Select(x => x.Split(new[] { ':' }, 2).Select(x => x.Trim()).ToArray())
					.Where(x => x.Length == 2)
					.Select(x => (key: x[0], value: x[1])))
				{
					switch (key)
					{
						case "Instance ID": DeviceInstanceId = value; break;
						case "Device Description": Description = value; break;
						case "Class Name": ClassName = value; break;
						case "Class GUID":
							try
							{
								ClassGuid = new Guid(value);
							}
							catch (FormatException)
							{
							}
							break;
						case "Manufacturer Name": Manufacturer = value; break;
						case "Status": Status = value; break;
					}
				}

				IsEnabled = !string.IsNullOrEmpty(Status)
						 && !Status.Equals("Disabled", StringComparison.OrdinalIgnoreCase);
			}
		}

		#endregion

		public static async Task<CameraItem[]> GetCamerasAsync()
		{
			return (await GetCamerasAsync("Camera"))
				.Concat(await GetCamerasAsync("Image")).ToArray();
		}

		public static async Task<CameraItem[]> GetCamerasAsync(string className)
		{
			var lines = await ExecuteCommandLineAsync($"/enum-devices /class {className} /connected");

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

		private static async Task<string[]> ExecuteDirectAsync(string arguments)
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

		private static async Task<string[]> ExecuteCommandLineAsync(string arguments)
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
}