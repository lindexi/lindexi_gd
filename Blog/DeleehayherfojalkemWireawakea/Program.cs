// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

using DeleehayherfojalkemWireawakea;

//Debugger.Launch();

var imageManager = new ImageManager();

// Token 申请：https://i.cnblogs.com/settings
var key = File.ReadAllText(@"C:\lindexi\CA\博客园密码");

var cnBlogsImageUploader = new CnBlogsImageUploader()
{
    BlogId = "lindexi",
    UserName = "lindexi",
    Key = key
};

var originFolder = new DirectoryInfo(@"C:\lindexi\Work\");
var workFolder = new DirectoryInfo(@"C:\lindexi\Work\");

if (args.Length == 2)
{
    originFolder = new DirectoryInfo(args[0]);
    workFolder = new DirectoryInfo(args[1]);
}

Log.WriteLine($"OriginFolder={originFolder.FullName}");
Log.WriteLine($"WorkFolder={workFolder.FullName}");

var imageManagerFile = new FileInfo(Path.Join(workFolder.FullName, "Image.json"));
var backupFileName = string.Join("", SHA1.HashData(Encoding.UTF8.GetBytes(imageManagerFile.FullName)).Select(t => t.ToString("X2"))) +
                     ".json";
var backupFile = new FileInfo(Path.Join(AppContext.BaseDirectory, backupFileName));
// 由于博客文件夹会被删除，因此需要一个备份文件才能工作，备份文件放在程序运行目录
if (imageManagerFile.Exists)
{
    // 理论上不会进入，因为文件被删除
    imageManager.Deserialize(imageManagerFile);
}
else if (backupFile.Exists)
{
    imageManager.Deserialize(backupFile);
}

var imageProvider = new ImageProvider()
{
    OriginFolder = originFolder,
    CnBlogsImageUploader = cnBlogsImageUploader,
    ImageManager = imageManager
};

foreach (var blogFile in workFolder.EnumerateFiles("*.md", SearchOption.AllDirectories))
{
    Log.WriteLine($"开始转换 {blogFile}");
    imageProvider.Convert(blogFile);
    Log.WriteLine();
}

imageManager.Serialize(imageManagerFile);
imageManagerFile.CopyTo(backupFile.FullName, overwrite: true);