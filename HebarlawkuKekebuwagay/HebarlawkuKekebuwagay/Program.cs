
using System.Diagnostics;
using System.Reflection;

Console.WriteLine($"Fx {Environment.CurrentDirectory}");

if (args.Length > 0)
{
    return;
}

var location = Assembly.GetExecutingAssembly().Location;
var fileName = Path.GetFileNameWithoutExtension(location);
var directory = Path.GetDirectoryName(location);

var exe = Path.Combine(directory, fileName + ".exe");
var processStartInfo = new ProcessStartInfo(exe,"fx")
{
    WorkingDirectory = "Z:\\Windows"
};
var process = Process.Start(processStartInfo);
process.WaitForExit();
// System.ComponentModel.Win32Exception: 'An error occurred trying to start process' 目录名称无效
// The directory name is invalid
// [c# - Win32Exception: The directory name is invalid - Stack Overflow](https://stackoverflow.com/questions/990562/win32exception-the-directory-name-is-invalid )
