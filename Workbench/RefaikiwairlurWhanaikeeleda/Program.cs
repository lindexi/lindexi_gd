// See https://aka.ms/new-console-template for more information

using System.Formats.Tar;
using System.IO.Compression;

var file = @"E:\download\dotnet-runtime-8.0.17-linux-x64.tar.gz";

var output = Path.Join(AppContext.BaseDirectory, "CahohuneaFaidinalnalfaji");
Directory.CreateDirectory(output);
// System.IO.InvalidDataException:“Unable to parse number.”
//TarFile.ExtractToDirectory(file, output, overwriteFiles: true);
var fileStream = File.OpenRead(file);

var gZipStream = new GZipStream(fileStream,CompressionMode.Decompress);
//var tarReader = new TarReader(gZipStream);
//TarEntry? tarEntry = await tarReader.GetNextEntryAsync();

TarFile.ExtractToDirectory(gZipStream, output, overwriteFiles: true);

Console.WriteLine("Hello, World!");
