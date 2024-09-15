using System.Diagnostics.CodeAnalysis;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace DeleehayherfojalkemWireawakea;

class ImageManager
{
    public void AddImageUrl(string localFile, string url)
    {
        _dictionary[localFile] = url;
    }

    public bool TryGetImageUrl(string localFile, [NotNullWhen(true)] out string? url)
    {
        return _dictionary.TryGetValue(localFile, out url);
    }

    private readonly Dictionary<string/*本地文件*/, string/*博客园下载地址*/> _dictionary = [];

    public void Serialize(FileInfo file)
    {
        using var fileStream = file.OpenWrite();
        fileStream.SetLength(0);
        JsonSerializer.Serialize(fileStream, _dictionary,new JsonSerializerOptions()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true,
        });
    }

    public void Deserialize(FileInfo file)
    {
        using var fileStream = file.OpenRead();
        var dictionary = JsonSerializer.Deserialize<Dictionary<string/*本地文件*/, string/*博客园下载地址*/>>(fileStream);
        _dictionary.Clear();
        if (dictionary == null)
        {
            return;
        }

        foreach (var (key, value) in dictionary)
        {
            _dictionary[key] = value;
        }
    }
}