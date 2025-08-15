// See https://aka.ms/new-console-template for more information

using FafurbaliHerekaylarnerecerne;


ShellHelper.ToggleDesktop();

var linkFile = @"C:\lindexi\快捷方式.lnk";
Console.WriteLine(ShellHelper.GetLinkTargetPath(new FileInfo(linkFile)));