using System.ComponentModel;

namespace HelloSwitcher.Service;

[RunInstaller(true)]
public partial class ProjectInstaller : System.Configuration.Install.Installer
{
	public ProjectInstaller()
	{
		InitializeComponent();
	}
}