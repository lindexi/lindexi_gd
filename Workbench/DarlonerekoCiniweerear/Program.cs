// See https://aka.ms/new-console-template for more information
var foo = new object[3];
foo[0] = 1;
foo[1] = "lindex";
foo[2] = new object();

Array.Clear(foo);

Console.WriteLine("Hello, World!");
