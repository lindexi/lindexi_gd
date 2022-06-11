// See https://aka.ms/new-console-template for more information

using ICSharpCode.SharpZipLib.Zip;

var file = @"d:\temp\e4af850bd4af435ea5e7b153a2e8a8e7.pptx";

var fastZipEvents = new FastZipEvents();
fastZipEvents.Progress += (sender, eventArgs) =>
{

};

var fastZip = new FastZip();
fastZip.ExtractZip(file, "zip", FastZip.Overwrite.Always, name =>
{
    return true;
}, "", "", false);

Console.WriteLine("Hello, World!");
