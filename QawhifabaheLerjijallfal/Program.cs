// See https://aka.ms/new-console-template for more information

using System.Text;

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var encoding = Encoding.GetEncoding("GBK");
var text = encoding.GetString(new byte[] { 0xcb, 0xe6,0xd7,0xc5 });

Console.WriteLine("Hello, World!");
