using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace HelloSwitcher.Models
{
	/// <summary>
	/// Utility methods by DevCon
	/// </summary>
	/// <remarks>
	/// https://docs.microsoft.com/en-us/windows-hardware/drivers/devtest/devcon
	/// </remarks>
	internal static class DeviceConsole
	{
		private const string DevconFileName = "Devcon.exe";

		public static Task<bool> EnableAsync(string deviceId) => ExecuteAsync($@"enable ""{deviceId}""", "Enabled");
		public static Task<bool> DisableAsync(string deviceId) => ExecuteAsync($@"disable ""{deviceId}""", "Disabled");

		private static async Task<bool> ExecuteAsync(string arguments, string expectedResult)
		{
			var actualResults = await ExecuteBaseAsync(arguments);
#if DEBUG
			foreach (var result in actualResults)
				Debug.WriteLine($"RE: {result}");
#endif
			return actualResults
				.Where(x => !string.IsNullOrEmpty(x))
				.Any(x => x.LastIndexOf(expectedResult, StringComparison.OrdinalIgnoreCase) >= 0);
		}

		private static async Task<string[]> ExecuteBaseAsync(string arguments)
		{
			var folderPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			var filePath = Path.Combine(folderPath, DevconFileName);

			if (!File.Exists(filePath))
				return Array.Empty<string>();

			using var proc = new Process
			{
				StartInfo =
				{
					FileName = filePath,
					Arguments = arguments,
					Verb = "runas",
					CreateNoWindow = true,
					UseShellExecute = false,
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
			catch (Exception ex)
			{
				Debug.WriteLine(ex);
				return outputLines.ToArray();
			}
			finally
			{
				proc.OutputDataReceived -= received;
				proc.Exited -= exited;
			}
		}
	}
}