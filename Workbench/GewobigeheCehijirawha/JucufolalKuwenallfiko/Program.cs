// See https://aka.ms/new-console-template for more information

using System.Reflection;
using System.Runtime.Loader;

#if DEBUG
var configuration = "Debug";
#else
var configuration = "Release";
#endif

var dllFile = Path.Join(AppContext.BaseDirectory,
    @$"..\..\KarnadikemnemkaCallcilowhijinem\{configuration}\KarnadikemnemkaCallcilowhijinem.dll");
dllFile = Path.GetFullPath(dllFile);

Console.WriteLine($"测试加载 {dllFile}");

if (!File.Exists(dllFile))
{
}

var t = LoadAndUnloadAssembly();

for (int i = 0; t.IsAlive; i++)
{
    GC.Collect();
    GC.WaitForFullGCComplete();
    GC.Collect();

    Thread.Sleep(1000);

    if (i > 100)
    {
        Console.WriteLine($"等不到释放");
        return;
    }
}

Thread.Sleep(1000);

File.Delete(dllFile);

Console.WriteLine($"成功删除文件 {dllFile}");

WeakReference LoadAndUnloadAssembly()
{
    var assemblyLoadContext = new AssemblyLoadContext("Test", isCollectible: true);
    Assembly assembly = assemblyLoadContext.LoadFromAssemblyPath(dllFile);
    var fooType = assembly.GetType("KarnadikemnemkaCallcilowhijinem.Foo")!;
    var foo = Activator.CreateInstance(fooType);
    var methodInfo = fooType.GetMethod("Do")!;
    methodInfo.Invoke(foo, null);
    try
    {
        File.Delete(dllFile);
        Console.WriteLine($"立刻删除程序集文件成功");
    }
    catch (UnauthorizedAccessException e)
    {

    }
    assemblyLoadContext.Unload();
    return new WeakReference(assemblyLoadContext);
}