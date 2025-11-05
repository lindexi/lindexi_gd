// See https://aka.ms/new-console-template for more information

var file = @"c:\Users\lindexi\AppData\Local\Microsoft\WindowsApps\Microsoft.FoundryLocal_8wekyb3d8bbwe\foundry.exe";
var target = Walterlv.IO.PackageManagement.Core.JunctionPoint.GetTarget(file);
Console.WriteLine(target);
