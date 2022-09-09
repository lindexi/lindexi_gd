var (foo1, foo2) = await (GetFoo1Async(), GetFoo2Async());

Console.WriteLine(foo1);
Console.WriteLine(foo2);

Task<string> GetFoo1Async() => Task.Run(() => "Foo1");

Task<string> GetFoo2Async() => Task.Run(() => "Foo2");