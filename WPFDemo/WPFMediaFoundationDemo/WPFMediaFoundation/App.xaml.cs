using System.Windows;
using System.Windows.Threading;

namespace WPFMediaFoundation
{
    /// <summary>
    /// Reports playback errors without blocking unattended tests with a dialog.
    /// </summary>
    public partial class App : Application
    {
        /// <inheritdoc />
        protected override void OnStartup(StartupEventArgs e)
        {
            Environment.ExitCode = e.Args.Contains("--self-test", StringComparer.Ordinal) ? 1 : 0;
            DispatcherUnhandledException += OnDispatcherUnhandledException;
            base.OnStartup(e);
        }

        /// <inheritdoc />
        protected override void OnExit(ExitEventArgs e)
        {
            if (Environment.ExitCode != 0) e.ApplicationExitCode = Environment.ExitCode;
            Console.WriteLine($"EXIT code={e.ApplicationExitCode}");
            base.OnExit(e);
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Console.Error.WriteLine($"ERROR {e.Exception}");
            e.Handled = true;
            Environment.ExitCode = 1;
            try
            {
                MainWindow?.Close();
            }
            finally
            {
                Shutdown(1);
            }
        }
    }
}
