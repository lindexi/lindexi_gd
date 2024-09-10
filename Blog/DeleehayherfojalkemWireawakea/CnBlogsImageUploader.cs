using MetaWeblogClient;

namespace DeleehayherfojalkemWireawakea;

class CnBlogsImageUploader
{
    public required string Key { get; init; }
    public required string BlogId { get; init; }
    public required string UserName { get; init; }

    public string UploadImage(string imageFile)
    {
        var blogConnectionInfo = new BlogConnectionInfo("https://www.cnblogs.com/" + BlogId, "https://rpc.cnblogs.com/metaweblog/" + BlogId,
            BlogId,
            UserName, Key);
        var blogClient = new Client(blogConnectionInfo);

        var extension = Path.GetExtension(imageFile);
        string mime;
        if (string.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase))
        {
            mime = "image/png";
        }
        else if (string.Equals(extension, ".jpg", StringComparison.OrdinalIgnoreCase))
        {
            mime = "image/jpeg";
        }
        else if (string.Equals(extension, ".gif", StringComparison.OrdinalIgnoreCase))
        {
            mime = "image/gif";
        }
        else if (string.Equals(extension, ".bmp", StringComparison.OrdinalIgnoreCase))
        {
            mime = "image/bmp";
        }
        else if (string.Equals(extension, ".webp", StringComparison.OrdinalIgnoreCase))
        {
            mime = "image/webp";
        }
        else
        {
            throw new ArgumentException($"不支持的图片格式 {extension}");
        }

        var mediaObjectInfo = blogClient.NewMediaObject(Path.GetFileName(imageFile), mime,
            File.ReadAllBytes(imageFile));
        var url = mediaObjectInfo.URL;
        return url;
    }
}