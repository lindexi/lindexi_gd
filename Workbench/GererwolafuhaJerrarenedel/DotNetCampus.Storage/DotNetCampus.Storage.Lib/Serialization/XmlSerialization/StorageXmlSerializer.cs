using System.Text.Json;
using System.Xml;
using System.Xml.Linq;

namespace DotNetCampus.Storage.Lib;

public class StorageXmlSerializer
{
    public StorageNode Parse()
    {
        return null!;
    }

    // Serialize

    public async Task DeserializeAsync(FileInfo file)
    {
        //var xmlDocument = new XmlDocument();
        using var fileStream = file.OpenRead();
        XDocument document = await XDocument.LoadAsync(fileStream, LoadOptions.None, CancellationToken.None);
    }
}