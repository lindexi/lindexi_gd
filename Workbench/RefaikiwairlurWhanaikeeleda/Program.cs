// See https://aka.ms/new-console-template for more information

using System.Formats.Tar;

var file = @"E:\download\dotnet-runtime-8.0.17-linux-x64.tar.gz";

var output = Path.Join(AppContext.BaseDirectory, "CahohuneaFaidinalnalfaji");
Directory.CreateDirectory(output);
// System.IO.InvalidDataException:“Unable to parse number.”
//TarFile.ExtractToDirectory(file, output, overwriteFiles: true);
var fileStream = File.OpenRead(file);
var tarReader = new TarReader(fileStream);
TarEntry? tarEntry = await tarReader.GetNextEntryAsync();


Console.WriteLine("Hello, World!");
