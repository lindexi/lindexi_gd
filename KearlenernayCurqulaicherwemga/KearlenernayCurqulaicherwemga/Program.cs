// See https://aka.ms/new-console-template for more information

using System.Runtime.InteropServices;

Console.WriteLine(Add(1, 1));

[DllImport("FaryubawkaJebelchako.dll")]
static extern int Add(int a, int b);