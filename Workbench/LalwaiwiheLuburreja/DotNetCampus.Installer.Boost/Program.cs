// See https://aka.ms/new-console-template for more information

using System.Reflection;
using System.Runtime.InteropServices;

Console.WriteLine($"Hello, World!测试中文 RuntimeIdentifier={RuntimeInformation.RuntimeIdentifier} FrameworkDescription={RuntimeInformation.FrameworkDescription}");

var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("DotNetCampus.Installer.Boost.Assets.zip")!;
Console.WriteLine(stream.Length);