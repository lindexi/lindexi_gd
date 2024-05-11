// See https://aka.ms/new-console-template for more information

using System.CommandLine.IO;
using System.CommandLine.Rendering;
using System.CommandLine.Rendering.Views;

var gridView = new GridView()
{
};
gridView.SetColumns(ColumnDefinition.Fixed(100), ColumnDefinition.Star(1), ColumnDefinition.Star(1));
gridView.SetChild(new ContentView("123"),0,0);
gridView.SetChild(new ContentView("22"),1,0);
gridView.SetChild(new ContentView("123"),2, 0);
var consoleRenderer = new ConsoleRenderer(new SystemConsole());
gridView.Render(consoleRenderer, new Region(10,10,200,100));

Console.Read();
