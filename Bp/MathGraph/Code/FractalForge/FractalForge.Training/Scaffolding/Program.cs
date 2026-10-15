using System.Reflection;
using System.Resources;

var resources = new ResourceManager(
    "FractalForge.Training.Scaffolding.Messages",
    Assembly.Load("FractalForge.Training"));
Console.Error.WriteLine(resources.GetString("ResearchNotImplemented"));
return 2;
