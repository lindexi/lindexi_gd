// See https://aka.ms/new-console-template for more information

using System.Runtime.CompilerServices;

var a = 1;
var b = 2;
Foo(a > b);

Console.WriteLine("Hello, World!");


void Foo(bool f, [CallerArgumentExpression("f")] string expression = null!)
{

}