// See https://aka.ms/new-console-template for more information

using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text;

var file = @"C:\Windows\System32\ntdll.dll";

using var fileStream = File.OpenRead(file);

var peReader = new PEReader(fileStream);

foreach (var debugDirectoryEntry in peReader.ReadDebugDirectory())
{
    var readCodeViewDebugDirectoryData = peReader.ReadCodeViewDebugDirectoryData(debugDirectoryEntry);
    var path = readCodeViewDebugDirectoryData.Path;
    var guid = readCodeViewDebugDirectoryData.Guid;
    var age = readCodeViewDebugDirectoryData.Age;

    var pdbName = path;

    var downloadUrl = $"http://msdl.microsoft.com/download/symbols/{pdbName}/{(guid.ToString("N").ToUpperInvariant() + age.ToString())}/{pdbName}";
    var httpClient = new HttpClient();
    using var httpResponseMessage = await httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);

    var pdbFile = Path.GetFullPath(pdbName);
    await using var downloadFileStream = File.Create(pdbFile);
    await httpResponseMessage.Content.CopyToAsync(downloadFileStream);
}

var peHeaders = new PEHeaders(fileStream);
var peHeader = peHeaders.PEHeader!;

var debugTableDirectory = peHeader.DebugTableDirectory;


var seek = fileStream.Seek(debugTableDirectory.RelativeVirtualAddress, SeekOrigin.Begin);
var buffer = new byte[debugTableDirectory.Size];
var readCount = fileStream.Read(buffer, 0, buffer.Length);

var offset = 4 + Marshal.SizeOf<Guid>() + sizeof(uint);
var allSize = offset + 255;

var slice = buffer.AsSpan().Slice(offset);

var unicodeText = Encoding.Unicode.GetString(buffer.AsSpan());

var text = Encoding.UTF32.GetString(buffer.AsSpan());

Console.WriteLine("Hello, World!");
