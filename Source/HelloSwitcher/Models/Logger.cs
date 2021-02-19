using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HelloSwitcher.Models
{
	internal static class Logger
	{
		private static string FolderPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), nameof(HelloSwitcher));
		private static string OperationFilePath => Path.Combine(FolderPath, "operation.txt");
		private static string ExceptionFilePath => Path.Combine(FolderPath, "exception.txt");

		private static void EnsureFolder()
		{
			if (!Directory.Exists(FolderPath))
				Directory.CreateDirectory(FolderPath);
		}

		public static void RecordOperation(string content, bool append = true)
		{
			EnsureFolder();

			using var writer = new StreamWriter(OperationFilePath, append, Encoding.UTF8);
			writer.Write($"[{DateTime.Now}]{Environment.NewLine}{content}{Environment.NewLine}{Environment.NewLine}");
		}

		public static void RecordException(object exception)
		{
			EnsureFolder();

			File.WriteAllText(ExceptionFilePath, $"[{DateTime.Now}]{Environment.NewLine}{exception}");
		}
	}
}