// See https://aka.ms/new-console-template for more information

using Microsoft.VisualStudio.SolutionPersistence.Model;
using Microsoft.VisualStudio.SolutionPersistence.Serializer;

var slnxFile = @"C:\lindexi\Work\Foo.slnx";
var slnFile = @"C:\lindexi\Work\Foo.sln";

var solutionModel = await SolutionSerializers.SlnXml.OpenAsync(slnxFile, CancellationToken.None);

await SolutionSerializers.SlnFileV12.SaveAsync(slnFile, solutionModel, CancellationToken.None);

Console.WriteLine("Hello, World!");
