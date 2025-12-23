// See https://aka.ms/new-console-template for more information
using CacalwewuWeficawherebenearle;

var shortcutFile = Path.Join(AppContext.BaseDirectory, "6.lnk");
var iconFile = @"C:\lindexi\不存在\Icon.ico";

ShortcutHelper.CreateShortcut(shortcutFile, "https://blog.lindexi.com/", Directory.GetCurrentDirectory(), iconFile: iconFile);

Console.WriteLine("Hello, World!");

Console.Read();