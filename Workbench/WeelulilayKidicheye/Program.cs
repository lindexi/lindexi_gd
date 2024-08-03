// See https://aka.ms/new-console-template for more information
using MimeTypes;

var mimeType = MimeTypeMap.GetMimeType("png");
Console.WriteLine(mimeType);

while (true)
{
    var extension = Console.ReadLine();
    Console.WriteLine(MimeTypeMap.GetMimeType(extension));
}

Console.WriteLine("Hello, World!");
