// See https://aka.ms/new-console-template for more information

using System.IO.Pipes;

AppDomain.CurrentDomain.FirstChanceException += (sender, eventArgs) =>
{
    Console.WriteLine($"FirstChanceException: {eventArgs.Exception.GetType().FullName} {eventArgs.Exception.Message}");
};

var namedPipeClientStream = new NamedPipeClientStream("DoNotExistFoo");
namedPipeClientStream.Connect();

Console.WriteLine("Hello, World!");