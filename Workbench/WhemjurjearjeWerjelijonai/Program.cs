// See https://aka.ms/new-console-template for more information

using CearcurwaidereFerkearjurnawhea;
using WhemjurjearjeWerjelijonai;

var class1 = new Class1();
Console.WriteLine($"Hello, World! {class1.Foo()}");
new Foo(class1).Do();

class Foo
{
    public Foo(Class1 class1)
    {
        _class1 = class1;
    }

    private readonly Class1 _class1;

    public void Do()
    {
        if (OperatingSystem.IsAndroid())
        {
            Task.Run(() =>
            {
                _class1.Foo();
                Console.WriteLine(typeof(ProgramAttribute));
            });

            Console.ReadLine();
        }
    }
}
