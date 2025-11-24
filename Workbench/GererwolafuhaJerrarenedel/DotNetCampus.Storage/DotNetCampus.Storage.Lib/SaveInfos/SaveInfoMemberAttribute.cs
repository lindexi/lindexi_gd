namespace DotNetCampus.Storage.Lib.SaveInfos;

[AttributeUsage(AttributeTargets.Property)]
public class SaveInfoMemberAttribute : Attribute
{
    public SaveInfoMemberAttribute(string name)
    {
        Name = name;
    }

    /// <summary>
    /// 存储名
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 描述信息
    /// </summary>
    public string? Description { get; set; }
}