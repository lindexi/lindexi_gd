// See https://aka.ms/new-console-template for more information
using BecearyernaDurfodejefela;

var shortcutFile = Path.Join(AppContext.BaseDirectory, "1.lnk");

// 尽管文档上明确说明不能用来创建指向 URL 的快捷方式，但实际上是可以的
// > This interface cannot be used to create a link to a URL.
ShortcutHelper.CreateShortcut(shortcutFile, "https://blog.lindexi.com/", Directory.GetCurrentDirectory());

Console.WriteLine("Hello, World!");

Console.Read();