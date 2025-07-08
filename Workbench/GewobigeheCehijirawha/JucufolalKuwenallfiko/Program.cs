// See https://aka.ms/new-console-template for more information

using System.Reflection;
using System.Runtime.Loader;

var dllFile = Path.Join(AppContext.BaseDirectory,
    @"..\..\KarnadikemnemkaCallcilowhijinem\debug\KarnadikemnemkaCallcilowhijinem.dll");
dllFile = Path.GetFullPath(dllFile);

if (!File.Exists(dllFile))
{
}

LoadAndUnloadAssembly();

for (int i = 0; i < 5; i++)
{
    GC.Collect();
    GC.WaitForFullGCComplete();
    GC.Collect();
}

File.Delete(dllFile);

Console.WriteLine("Hello, World!");

void LoadAndUnloadAssembly()
{
    var assemblyLoadContext = new TestAssemblyLoadContext();
    Assembly assembly = assemblyLoadContext.LoadFromAssemblyPath(dllFile);
    var fooType = assembly.GetType("KarnadikemnemkaCallcilowhijinem.Foo")!;
    var foo = Activator.CreateInstance(fooType);
    var methodInfo = fooType.GetMethod("Do")!;
    methodInfo.Invoke(foo, null);
    try
    {
        File.Delete(dllFile);
    }
    catch (UnauthorizedAccessException e)
    {

    }
    assemblyLoadContext.Unload();
}

class TestAssemblyLoadContext : AssemblyLoadContext
{
    public TestAssemblyLoadContext() : base(true)
    {
    }
    protected override Assembly? Load(AssemblyName name)
    {
        return null;
    }
}