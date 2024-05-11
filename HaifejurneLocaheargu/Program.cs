// See https://aka.ms/new-console-template for more information

Directory.CreateDirectory("Foo1");
Directory.CreateDirectory(@"Foo1\Foo2");
Directory.CreateDirectory(@"Foo1\Foo2\Foo3");

foreach (var directory in Directory.EnumerateDirectories("Foo1", "*",SearchOption.AllDirectories))
{
    Console.WriteLine(directory);
}

Console.WriteLine("Hello, World!");
