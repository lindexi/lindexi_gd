// See https://aka.ms/new-console-template for more information

using System.Runtime.ExceptionServices;

var exception = new ArgumentException();
ExceptionDispatchInfo.SetRemoteStackTrace(exception, $@"栽赃的堆栈.{nameof(Foo)}.{nameof(Foo.F1)}
Main");
Console.WriteLine(exception);

class Foo
{
    public void F1()
    {
    }
}