namespace DotNetCampus.Storage.Analyzer;

record SaveInfoPropertyInfo
{
    public required string PropertyName { get; init; }
    public string PropertyType => TypeInfo.PropertyType;
    public required string StorageName { get; init; }
    //public required bool IsNullable { get; init; }
    public IReadOnlyList<string>? Aliases { get; init; }

    public required SaveInfoPropertyTypeInfo TypeInfo { get; init; }
}

readonly record struct SaveInfoPropertyTypeInfo
{
    public required string PropertyType { get; init; }

    /// <summary>
    /// 是否列表类型
    /// </summary>
    public required bool IsListType { get; init; }

    /// <summary>
    /// 列表类型里面的泛型类型名称
    /// </summary>
    public required string? ListGenericType { get; init; }

    /// <summary>
    /// 是否枚举类型
    /// </summary>
    public required bool IsEnumType { get; init; }

    /// <summary>
    /// 是否可空的值类型
    /// </summary>
    public required bool IsNullableValueType { get; init; }
}