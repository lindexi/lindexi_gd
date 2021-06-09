using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Lsj.Util.Win32;

namespace BerekunakeaLearweekacee
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            Shell32.SetCurrentProcessExplicitAppUserModelID(AppId);

            base.OnStartup(e);
        }

        private const string AppId = "lindexi is doubi";
    }
}
