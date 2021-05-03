
using System.Collections;

namespace HelloSwitcher.Service
{
	partial class ProjectInstaller
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		public const string ArgumentsOption = "/Arguments=";

		protected override void OnBeforeInstall(IDictionary savedState)
		{
			var path = this.Context.Parameters["assemblypath"];
			var arguments = this.Context.Parameters[ArgumentsOption.TrimStart('/').TrimEnd('=')]?.Trim();
			if (!string.IsNullOrEmpty(arguments))
				this.Context.Parameters["assemblypath"] = $"\"{path.Trim('\"')}\" {arguments}";

			base.OnBeforeInstall(savedState);
		}

		#region Component Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.HelloSwitcherServiceProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
			this.HelloSwitcherServiceInstaller = new System.ServiceProcess.ServiceInstaller();
			// 
			// HelloSwitcherServiceProcessInstaller
			// 
			this.HelloSwitcherServiceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
			this.HelloSwitcherServiceProcessInstaller.Password = null;
			this.HelloSwitcherServiceProcessInstaller.Username = null;
			// 
			// HelloSwitcherServiceInstaller
			// 
			this.HelloSwitcherServiceInstaller.Description = "Helps switching cameras for Windows Hello";
			this.HelloSwitcherServiceInstaller.DisplayName = "Hello Switcher Service";
			this.HelloSwitcherServiceInstaller.ServiceName = "HelloSwitcherService";
			this.HelloSwitcherServiceInstaller.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
			// 
			// ProjectInstaller
			// 
			this.Installers.AddRange(new System.Configuration.Install.Installer[] {
			this.HelloSwitcherServiceProcessInstaller,
			this.HelloSwitcherServiceInstaller});

		}

		#endregion

		private System.ServiceProcess.ServiceProcessInstaller HelloSwitcherServiceProcessInstaller;
		private System.ServiceProcess.ServiceInstaller HelloSwitcherServiceInstaller;
	}
}