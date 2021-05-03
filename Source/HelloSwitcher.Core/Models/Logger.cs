using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HelloSwitcher.Models
{
	public class Logger
	{
		public static bool IsOperationEnabled = Environment.GetCommandLineArgs().Any(x => string.Equals(x, OperationOption, StringComparison.OrdinalIgnoreCase));
		public const string OperationOption = "/operation";

		private static string FolderPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), nameof(HelloSwitcher));

		private readonly string _operationFilePath;
		private readonly string _errorFilePath;

		public Logger(string operationFileName, string errorFileName)
		{
			if (string.IsNullOrWhiteSpace(operationFileName))
				throw new ArgumentNullException(nameof(operationFileName));
			if (string.IsNullOrWhiteSpace(errorFileName))
				throw new ArgumentNullException(nameof(errorFileName));

			_operationFilePath = Path.Combine(FolderPath, operationFileName);
			_errorFilePath = Path.Combine(FolderPath, errorFileName);
		}

		private static void EnsureFolder()
		{
			if (!Directory.Exists(FolderPath))
				Directory.CreateDirectory(FolderPath);
		}

		public void RecordOperation(string content, bool append = true)
		{
			if (!IsOperationEnabled)
				return;

			EnsureFolder();

			using var writer = new StreamWriter(_operationFilePath, append, Encoding.UTF8);
			writer.Write($"[{DateTime.Now:yyyy-MM-dd hh:mm:ss.fff}]{Environment.NewLine}{content}{Environment.NewLine}{Environment.NewLine}");
		}

		public void RecordError(object exception, bool append = true)
		{
			EnsureFolder();

			using var writer = new StreamWriter(_errorFilePath, append, Encoding.UTF8);
			writer.Write($"[{DateTime.Now:yyyy-MM-dd hh:mm:ss.fff}]{Environment.NewLine}{exception}{Environment.NewLine}{Environment.NewLine}");
		}
	}
}