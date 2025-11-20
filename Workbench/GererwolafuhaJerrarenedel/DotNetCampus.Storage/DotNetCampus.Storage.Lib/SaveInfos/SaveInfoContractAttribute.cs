namespace DotNetCampus.Storage.Lib.SaveInfos;

[AttributeUsage(AttributeTargets.Class)]
public class SaveInfoContractAttribute : Attribute
{
    public SaveInfoContractAttribute(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public string? Description { get; set; }
}