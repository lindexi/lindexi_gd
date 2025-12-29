namespace DotNetCampus.Storage.SaveInfos;

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

    /// <summary>
    /// 别名，用于兼容旧版本名称。作用就是发现某个版本的存储名写错了，想要修改为新的名称，但又想兼容旧名称时使用。只处理 旧->新 的情况，不处理 新->旧 的情况。即更改了存储名之后，旧版本将读取不到新名称的属性
    /// 为什么设计上不处理 新->旧 的情况？这是因为担心存在属性不正交问题。如果业务上确实需要兼容旧版本，请自行定义多个属性进行处理
    /// </summary>
    public string[]? Aliases { get; set; }
}