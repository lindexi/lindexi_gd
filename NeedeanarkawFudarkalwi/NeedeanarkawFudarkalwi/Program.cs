// See https://aka.ms/new-console-template for more information

using NeedeanarkawFudarkalwi;

var f2 = new F2();

int n = f2.Foo();

n = HotReloadMetadataUpdateHandler.HotReload;

Console.WriteLine(n);

Console.ReadLine();

class F2
{
    public int Foo()
    {
        var n = 2;
        return n;
    }
}