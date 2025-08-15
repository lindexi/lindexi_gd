// See https://aka.ms/new-console-template for more information
using FafurbaliHerekaylarnerecerne;

var shellComObject = new ShellComObject();
var dispatch = shellComObject.As<IShellDispatch>()!;
unsafe
{
    IDispatch* p;
    dispatch.Application(&p);

    dispatch.MinimizeAll();
    dispatch.FileRun();
}

ShellHelper.ToggleDesktop();

var linkFile = @"C:\lindexi\快捷方式.lnk";
Console.WriteLine(ShellHelper.GetLinkTargetPath(new FileInfo(linkFile)));