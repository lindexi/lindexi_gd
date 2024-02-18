// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");

double a, b, c, d;
a = b = c = d = 5;
var hashCode = new System.HashCode();
hashCode.Add(a);
hashCode.Add(b);
hashCode.Add(c);
hashCode.Add(d);
var code = hashCode.ToHashCode();
Console.WriteLine(code);