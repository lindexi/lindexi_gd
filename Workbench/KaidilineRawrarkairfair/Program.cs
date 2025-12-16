// See https://aka.ms/new-console-template for more information
using KaidilineRawrarkairfair;

var shortcutFile = Path.Join(AppContext.BaseDirectory, "1.lnk");
ShortcutHelper.CreateShortcut(shortcutFile, Environment.ProcessPath!, Directory.GetCurrentDirectory());

Console.WriteLine("Hello, World!");

Console.Read();