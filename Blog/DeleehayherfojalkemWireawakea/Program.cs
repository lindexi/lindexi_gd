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
var blogFile = new FileInfo(@"C:\lindexi\Work\WPF 记一个特别简单的点集滤波平滑方法.md");

var imageProvider = new ImageProvider()
{
    OriginFolder = originFolder,
    CnBlogsImageUploader = cnBlogsImageUploader,
    ImageManager = imageManager
};

imageProvider.Convert(blogFile);

Console.WriteLine("Hello, World!");
