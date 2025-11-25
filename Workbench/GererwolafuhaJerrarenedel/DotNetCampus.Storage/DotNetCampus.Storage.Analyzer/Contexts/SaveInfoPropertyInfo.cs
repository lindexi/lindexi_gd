namespace DotNetCampus.Storage.Analyzer;

record SaveInfoPropertyInfo
{
    public required string PropertyName { get; init; }
    public required string PropertyType { get; init; }
    public required string StorageName { get; init; }
    //public required bool IsNullable { get; init; }
    public IReadOnlyList<string>? Aliases { get; init; }

    /// <summary>
    /// 是否列表类型
    /// </summary>
    public bool IsListType { get; init; }

    /// <summary>
    /// 列表类型里面的泛型类型名称
    /// </summary>
    public string? ListGenericType { get; init; }

    public bool IsEnumType { get; init; }
}