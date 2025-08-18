using WharjeacheridajeNemkemnalldebair;

string raw = DumpDbgEngClient.RunDumpCommands(@"C:\lindexi\App.dmp", "!address -summary", "!vm");
Console.WriteLine(raw);
// 然后用正则或简单行解析抓取 Commit Total / Private 等字段

Console.WriteLine("Hello, World!");