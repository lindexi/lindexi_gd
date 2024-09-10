// See https://aka.ms/new-console-template for more information

using DeleehayherfojalkemWireawakea;

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

var imageManagerFile = new FileInfo(Path.Join(originFolder.FullName, "Image.json"));
if (imageManagerFile.Exists)
{
    imageManager.Deserialize(imageManagerFile);
}

var imageProvider = new ImageProvider()
{
    OriginFolder = originFolder,
    CnBlogsImageUploader = cnBlogsImageUploader,
    ImageManager = imageManager
};

foreach (var blogFile in workFolder.EnumerateFiles("*.md", SearchOption.AllDirectories))
{
    imageProvider.Convert(blogFile);
}

imageManager.Serialize(imageManagerFile);