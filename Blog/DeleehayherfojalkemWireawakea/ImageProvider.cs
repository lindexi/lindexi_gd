using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DeleehayherfojalkemWireawakea;

internal partial class ImageProvider
{
    public required DirectoryInfo OriginFolder { get; init; }

    public required ImageManager ImageManager { get; init; }

    public required CnBlogsImageUploader CnBlogsImageUploader { get; init; }

    [GeneratedRegex(@"<!--\s*!\[\]\(image/([\w /\.]*)\)\s-->")]
    public static partial Regex GetImageFileRegex();

    [GeneratedRegex(@"!\[\]\(http://cdn.lindexi.site/")]
    public static partial Regex GetImageLinkRegex();

    public void Convert(FileInfo blogFile)
    {
        var imageFileRegex = GetImageFileRegex();
        var imageLinkRegex = GetImageLinkRegex();

        bool isImage = false;
        var currentImageFile = "";
        var blogOutputText = new StringBuilder();

        foreach (var line in File.ReadLines(blogFile.FullName))
        {
            if (!isImage)
            {
                var match = imageFileRegex.Match(line);
                if (match.Success)
                {
                    currentImageFile = Path.Join(OriginFolder.FullName, "image", match.Groups[1].ValueSpan);
                    if (File.Exists(currentImageFile))
                    {
                        isImage = true;
                    }
                    else
                    {
                        Console.WriteLine($"本地文件找不到");
                    }
                }

                blogOutputText.AppendLine(line);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    blogOutputText.AppendLine();

                    continue;
                }

                var match = imageLinkRegex.Match(line);
                if (match.Success && !string.IsNullOrEmpty(currentImageFile) && File.Exists(currentImageFile))
                {
                    Console.WriteLine($"本地文件 {currentImageFile}");

                    var relativePath = Path.GetRelativePath(OriginFolder.FullName, currentImageFile);

                    if (!ImageManager.TryGetImageUrl(relativePath, out var url))
                    {
                        url = CnBlogsImageUploader.UploadImage(currentImageFile);
                        ImageManager.AddImageUrl(relativePath, url);
                    }

                    blogOutputText.AppendLine($"![]({url})");
                }
                else
                {
                    blogOutputText.AppendLine();
                }

                isImage = false;
                currentImageFile = null;
            }
        }
    }
}