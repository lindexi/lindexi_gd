// See https://aka.ms/new-console-template for more information

using System.CommandLine.IO;
using System.CommandLine.Rendering;
using System.CommandLine.Rendering.Views;

var systemConsoleTerminal = new SystemConsoleTerminal(new SystemConsole()
{

})
{

};
systemConsoleTerminal.Render(new ContentSpan("123"), new Region(10, 10, 100, 100));



Console.Read();
