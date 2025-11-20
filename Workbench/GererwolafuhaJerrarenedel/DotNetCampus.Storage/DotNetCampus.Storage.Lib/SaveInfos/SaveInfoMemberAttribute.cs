namespace DotNetCampus.Storage.Lib.SaveInfos;

[AttributeUsage(AttributeTargets.Property)]
public class SaveInfoMemberAttribute : Attribute
{
    public SaveInfoMemberAttribute(string name)
    {
        Name = name;
    }

    public string Name { get; set; }

    public string? Description { get; set; }
}