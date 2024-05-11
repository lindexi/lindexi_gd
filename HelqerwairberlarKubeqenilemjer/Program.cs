// See https://aka.ms/new-console-template for more information

using System.CommandLine.IO;
using System.CommandLine.Rendering;
using System.CommandLine.Rendering.Views;

var gridView = new GridView()
{
    new ContentView("123")
};
var consoleRenderer = new ConsoleRenderer(new SystemConsole());
gridView.Render(consoleRenderer, null);

Console.Read();
