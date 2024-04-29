// See https://aka.ms/new-console-template for more information
var file = @"f:\temp\NerairkeweNaircayyawhem\dotnet\Script\run.sh";
var text = File.ReadAllText(file);

text = text.Replace("\r\n", "\n");
File.WriteAllText(file, text);

Console.WriteLine("Hello, World!");
