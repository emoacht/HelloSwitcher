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
		private static string FolderPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), nameof(HelloSwitcher));

		private readonly string _operationFilePath;
		private readonly string _exceptionFilePath;

		public Logger(string operationFileName, string exceptionFileName)
		{
			if (string.IsNullOrWhiteSpace(operationFileName))
				throw new ArgumentNullException(nameof(operationFileName));
			if (string.IsNullOrWhiteSpace(exceptionFileName))
				throw new ArgumentNullException(nameof(exceptionFileName));

			_operationFilePath = Path.Combine(FolderPath, operationFileName);
			_exceptionFilePath = Path.Combine(FolderPath, exceptionFileName);
		}

		private static void EnsureFolder()
		{
			if (!Directory.Exists(FolderPath))
				Directory.CreateDirectory(FolderPath);
		}

		public void RecordOperation(string content, bool append = true)
		{
			EnsureFolder();

			using var writer = new StreamWriter(_operationFilePath, append, Encoding.UTF8);
			writer.Write($"[{DateTime.Now}]{Environment.NewLine}{content}{Environment.NewLine}{Environment.NewLine}");
		}

		public void RecordException(object exception)
		{
			EnsureFolder();

			File.WriteAllText(_exceptionFilePath, $"[{DateTime.Now}]{Environment.NewLine}{exception}");
		}
	}
}