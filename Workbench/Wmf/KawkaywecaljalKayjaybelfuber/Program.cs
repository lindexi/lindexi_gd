// See https://aka.ms/new-console-template for more information

using Oxage.Wmf;

var file = @"C:\lindexi\wmf公式\sample.wmf";

using var fileStream = File.OpenRead(file);
var wmfDocument = new WmfDocument();
wmfDocument.Load(fileStream);

var width = wmfDocument.Width;
var height = wmfDocument.Height;

var inch = wmfDocument.Width / 1440;
var pixel = inch * 96;

Console.WriteLine("Hello, World!");
