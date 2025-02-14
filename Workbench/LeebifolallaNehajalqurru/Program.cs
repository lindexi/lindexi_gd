// See https://aka.ms/new-console-template for more information

//UriParser.Register(new Foo(),"file",0);

using System.IO.Pipes;

var uri = new Uri(@"file://./pipe/");
Console.WriteLine("Hello, World!");

var namedPipeClientStream = new NamedPipeClientStream(@"\\.\pipe\xx");


class Foo : UriParser
{
    protected override string GetComponents(Uri uri, UriComponents components, UriFormat format)
    {
        return base.GetComponents(uri, components, format);
    }
}