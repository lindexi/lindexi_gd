using DotNetCampus.Storage.Lib.SaveInfos;

namespace DotNetCampus.Storage.Demo.SaveInfos;

[SaveInfoContract("Foo1")]
public class Foo1SaveInfo : SaveInfo
{
    [SaveInfoMember("Foo1Property", Description = "This is a foo1 property.")]
    public bool Foo1Property { get; set; } = false;

    [SaveInfoMember("Foo2Property", Description = "This is a foo2 property.")]
    public int Foo2Property { get; set; } = 3;
}