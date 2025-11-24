namespace DotNetCampus.Storage.Analyzer;

record SaveInfoPropertyInfo
{
    public required string PropertyName { get; init; }
    public required string PropertyType { get; init; }
    public required string StorageName { get; init; }
    //public required bool IsNullable { get; init; }
    public IReadOnlyList<string>? Aliases { get; init; }
    public bool IsListType { get; init; }
}