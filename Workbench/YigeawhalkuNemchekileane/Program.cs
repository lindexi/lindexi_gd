// See https://aka.ms/new-console-template for more information

using System.Diagnostics.CodeAnalysis;
using System.Text;

TextWriter writer = new C();
Console.WriteLine("Hello, World!");

class C:TextWriter
{
    public override Encoding Encoding { get; }

    [AllowNull] public override string NewLine { get; set; }
}