// See https://aka.ms/new-console-template for more information

using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text;

var file = @"ntdll.dll";

using var fileStream = File.OpenRead(file);

var peReader = new PEReader(fileStream);
var httpClient = new HttpClient();

var debugDirectoryEntries = peReader.ReadDebugDirectory();
foreach (var debugDirectoryEntry in debugDirectoryEntries)
{
    if (debugDirectoryEntry.Type != DebugDirectoryEntryType.CodeView)
    {
        continue;
    }

    var readCodeViewDebugDirectoryData = peReader.ReadCodeViewDebugDirectoryData(debugDirectoryEntry);
    var path = readCodeViewDebugDirectoryData.Path;
    var guid = readCodeViewDebugDirectoryData.Guid;
    var age = readCodeViewDebugDirectoryData.Age;

    var pdbName = path;

    var downloadUrl = $"http://msdl.microsoft.com/download/symbols/{pdbName}/{(guid.ToString("N").ToUpperInvariant() + age.ToString())}/{pdbName}";
    using var httpResponseMessage = await httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);

    var pdbFile = Path.GetFullPath(pdbName);
    await using var downloadFileStream = File.Create(pdbFile);
    await httpResponseMessage.Content.CopyToAsync(downloadFileStream);
}

Console.WriteLine("Hello, World!");
