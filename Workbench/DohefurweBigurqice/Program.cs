// See https://aka.ms/new-console-template for more information

var list = new List<Foo>();

for (int i = 0; i < 10; i++)
{
    list.Add(new Foo());
}

Foo? foo = list.Cast<Foo?>().FirstOrDefault(t => t.Value.N == -1, null);

Console.WriteLine("Hello, World!");


struct Foo
{
    public Foo()
    {
        _count++;
        N = _count;
    }
    public int N { get; }

    private static int _count = 0;
}