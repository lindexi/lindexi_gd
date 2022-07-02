// See https://aka.ms/new-console-template for more information

using System.Diagnostics.Runtime;
using Xamarin.Tools.Zip;

using (var zip = ZipArchive.Open(@"F:\temp\DeewhemkilaHerhinurhe.zip", FileMode.Open))
{

}

Console.WriteLine("Hello, World!");

var runtimeMetricsOptions = new RuntimeMetricsOptions()
{
    ThreadingEnabled = true,
    JitEnabled = true,
    GcEnabled = true,
};

var runtimeInstrumentation = new RuntimeInstrumentation(runtimeMetricsOptions);

using (runtimeInstrumentation)
{
}