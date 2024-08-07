// See https://aka.ms/new-console-template for more information

var foo = new Foo();

for (int i = 0; i < foo.GetCount(); i++)
{
    var value = foo[i];
    _ = value;
}

var fooCount = foo.GetCount();
for (int i = 0; i < fooCount; i++)
{
    var value = foo[i];
    _ = value;
}

for (int i = 0, count = foo.GetCount(); i < count; i++)
{
    var value = foo[i];
    _ = value;
}

Console.WriteLine("Hello, World!");

class Foo
{
    public string this[int index]
    {
        get
        {
           return "Hello" + index;
        }
    }

    public int GetCount()
    {
        // 模拟一个耗时的操作
        Thread.Sleep(100);

        return 100;
    }
}