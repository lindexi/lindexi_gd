// See https://aka.ms/new-console-template for more information

using Lindexi.Src.Core.IO;
using Lindexi.Src.GitCommand;

var slnPath = CodeSolutionHelper.GetCodeSolutionPath();
if (slnPath is null)
{
    return;
}

//var git = new Git(new DirectoryInfo(slnPath));
var programFile = Path.Join(slnPath, "Program.cs");
var fileLastModificationDate = Git.GetFileLastModificationDate(new FileInfo(programFile));
if (string.IsNullOrEmpty(fileLastModificationDate))
{
    // 没有更改过
}
else
{
    var lastModification = DateTime.Parse(fileLastModificationDate);
}


Console.WriteLine("Hello, World!");

