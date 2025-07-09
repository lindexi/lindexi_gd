using System.Text.Json;
using System.Xml;
using System.Xml.Linq;

namespace DotNetCampus.Storage.Lib;

public class StorageXmlSerializer
{
    // Serialize

    public async Task DeserializeAsync(FileInfo file)
    {
        using var fileStream = file.OpenRead();
        XDocument document = await XDocument.LoadAsync(fileStream, LoadOptions.None, CancellationToken.None);
        var root = document.Root!;

    }
}