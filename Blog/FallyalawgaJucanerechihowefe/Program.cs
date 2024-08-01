// See https://aka.ms/new-console-template for more information

using MetaWeblogClient;

using System.Text;
using System.Text.RegularExpressions;

var blogFolder = @"C:\lindexi\Blog\";

if (args.Length > 0)
{
    blogFolder = args[0];
}

var blogId = "lindexi";
var userName = "lindexi";

// Token 申请：https://i.cnblogs.com/settings
var key = File.ReadAllText(@"C:\lindexi\CA\博客园密码");

var blogConnectionInfo = new BlogConnectionInfo("https://www.cnblogs.com/"+ blogId, "https://rpc.cnblogs.com/metaweblog/" + blogId,
    blogId,
    userName, key);
var blogClient = new Client(blogConnectionInfo);
var usersBlogs = blogClient.GetUsersBlogs();


var textFile = Path.Join(blogFolder, "Windows 通过编辑注册表设置左右手使用习惯更改 Popup 弹出位置.md");
GetImageLink(textFile);

foreach (var blogFile in Directory.EnumerateFiles(blogFolder, "*.md"))
{
    var blogOutputText = new StringBuilder();


}

Console.WriteLine("Hello, World!");

void GetImageLink(string blogFile)
{
    var imageFileRegex = new Regex(@"<!--\s*!\[\]\(image/([\w /\.]*)\)\s-->");
    var imageLinkRegex = new Regex(@"!\[\]\(http://image.acmx.xyz/");

    bool isImage = false;
    var currentImageFile = "";
    var blogOutputText = new StringBuilder();

    foreach (var line in File.ReadLines(blogFile))
    {
        if (!isImage)
        {
            var match = imageFileRegex.Match(line);
            if (match.Success)
            {
                currentImageFile = Path.Join(blogFolder, "image", match.Groups[1].ValueSpan);
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
            if (match.Success)
            {
                Console.WriteLine($"本地文件 {currentImageFile}");

                var extension = Path.GetExtension(currentImageFile);
                string mime = "";
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
                    Console.WriteLine($"不支持的图片格式 {extension}");
                }

                if (!string.IsNullOrEmpty(mime))
                {
                    var mediaObjectInfo = blogClient.NewMediaObject(Path.GetFileName(currentImageFile), mime,
                        File.ReadAllBytes(currentImageFile));
                    var url = mediaObjectInfo.URL;
                    blogOutputText.AppendLine($"![]({url})");
                }
                else
                {
                    blogOutputText.AppendLine(
                        $"![](http://cdn.lindexi.com/{Uri.EscapeDataString(Path.GetFileName(currentImageFile) ?? string.Empty)})");
                }
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