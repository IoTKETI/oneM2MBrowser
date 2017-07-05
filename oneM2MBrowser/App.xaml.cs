using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Runtime.Remoting;
using System.Threading;
using Microsoft.Shell;

namespace MobiusResourceMonitor_sub
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    /// 

    public partial class App : Application, ISingleInstanceApp
    {
        private const string Unique = "xuehu0000@chennan2511050946055084639520023019";

        [STAThread]
        static void Main(string[] args)
        {
            if (SingleInstance<App>.InitializeAsFirstInstance(Unique))
            {
                var application = new App();
                application.InitializeComponent();
                application.Run();

                SingleInstance<App>.Cleanup();
            }

            return;
        }

        public bool SignalExternalCommandLineArgs(IList<string> args)
        {
            if (this.MainWindow.WindowState == WindowState.Minimized)
            {
                this.MainWindow.WindowState = WindowState.Normal;
            }

            this.MainWindow.Activate();

            return true;
        }
    }
}
