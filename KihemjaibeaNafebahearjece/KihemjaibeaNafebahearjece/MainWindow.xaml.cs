using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Input.StylusPlugIns;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace KihemjaibeaNafebahearjece
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            StylusPlugIns.Add(new Foo());

            StylusDown += MainWindow_StylusDown;

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        private void MainWindow_StylusDown(object sender, StylusDownEventArgs e)
        {
            Debug.WriteLine($"{DateTime.Now} StylusDown");
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            // We add `<legacyUnhandledExceptionPolicy enabled="1"/> ` to app.config that can make the application still run
        }
    }

    public class Foo : StylusPlugIn
    {
        /// <inheritdoc />
        protected override void OnStylusDown(RawStylusInput rawStylusInput)
        {
            throw new Exception("This exception will break the stylus input thread");
        }
    }
}
