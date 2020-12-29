using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Tobii.Interaction;
using Tobii.Interaction.Wpf;
using Tobii.Research;
using System.Drawing;

namespace HeatTracker
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private Host _host;
        MainWindow window;
        int wH, wW;
        private WpfInteractorAgent _wpfInteractorAgent;

        protected override void OnStartup(StartupEventArgs e)
        {
            _host = new Host();
            _wpfInteractorAgent = _host.InitializeWpfAgent();
             window = new MainWindow();
            window.Show();
            wH = (int)window.Height;
            wW = (int)window.Width;
            window.set_host(_host);
            window.set_wpfInteractorAgent(_wpfInteractorAgent);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _host.Dispose();
            base.OnExit(e);
        }
    }
}
