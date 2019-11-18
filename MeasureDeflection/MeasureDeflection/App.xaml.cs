using MeasureDeflection.Utils;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace MeasureDeflection
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public MainWindowViewModel _mvvm { get; set; }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            var fHandler = new FileHandler();
            _mvvm = new MainWindowViewModel(fHandler);
            MainWindowView vm = new MainWindowView(_mvvm);

            vm.Show();
        }
    }
}
