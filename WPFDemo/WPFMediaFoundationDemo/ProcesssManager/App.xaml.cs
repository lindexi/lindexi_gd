using System.Runtime.InteropServices;
using System.Windows;

namespace ProcesssManager;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        if (e.Args.Contains("--self-test"))
        {
            var processes = ProcessSnapshot.Capture();
            var current = processes.Single(p => p.ProcessID == (uint)Environment.ProcessId);
            if (current.WorkingSetSize <= 0) throw new InvalidOperationException("Current process memory is unavailable.");
            Console.WriteLine($"PASS processes={processes.Count}, current={current.ProcessID}, workingSet={current.WorkingSetSize}, architecture={RuntimeInformation.ProcessArchitecture}");
            Shutdown(0);
            return;
        }
        base.OnStartup(e);
        new MainWindow().Show();
    }
}
