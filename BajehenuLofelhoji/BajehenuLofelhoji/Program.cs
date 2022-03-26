// See https://aka.ms/new-console-template for more information

var nasSetupSubRepositoryRelativeFolderPath=@"Foo 1\Foo 2";

var fileName="Foo 3应用.exe";

var relative = Path.Combine(nasSetupSubRepositoryRelativeFolderPath,fileName);

var url = "http://download.lindexi.com";

var uri = new Uri(url);

Uri.TryCreate(uri, relative, out var result);

var t = result.AbsoluteUri;

Console.Read();