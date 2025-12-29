using DotNetCampus.Storage.SaveInfos;

namespace DotNetCampus.Storage.Demo.SaveInfos;

[SaveInfoContract("Foo1")]
public class DemoFoo1SaveInfo : DemoBaseSaveInfo
{
    [SaveInfoMember("Foo1Property", Description = "This is a foo1 property.")]
    public bool Foo1Property { get; set; } = false;

    [SaveInfoMember("Foo2Property", Description = "This is a foo2 property.")]
    public int Foo2Property { get; set; } = 3;

    [SaveInfoMember("F3Fx", Aliases = ["Foo3Fx", "FxFoo"], Description = "This is a foo3 property.")]
    public int FooFxxProperty { get; set; } = 5;
}

public class DemoBaseSaveInfo : SaveInfo
{
    [SaveInfoMember("Foo3Property", Description = "This is a foo3 property.")]
    public long Foo3Property { get; set; } = 3;
}