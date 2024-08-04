using System.ServiceProcess;

namespace HelloSwitcher.Service;

static class Program
{
	static void Main()
	{
		ServiceBase.Run(new HelloSwitcherService());
	}
}