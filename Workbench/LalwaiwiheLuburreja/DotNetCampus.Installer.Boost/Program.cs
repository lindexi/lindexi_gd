// See https://aka.ms/new-console-template for more information

using System.Reflection;
using System.Runtime.InteropServices;

Console.WriteLine($"Hello, World!测试中文 RuntimeIdentifier={RuntimeInformation.RuntimeIdentifier} FrameworkDescription={RuntimeInformation.FrameworkDescription}");

var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("DotNetCampus.Installer.Boost.Assets.zip")!;
Console.WriteLine(stream.Length);
var assetsZip = Path.Join(AppContext.BaseDirectory, "Assets.zip");
using var fileStream = new FileStream(assetsZip,FileMode.Create,FileAccess.Write);
stream.CopyTo(fileStream);