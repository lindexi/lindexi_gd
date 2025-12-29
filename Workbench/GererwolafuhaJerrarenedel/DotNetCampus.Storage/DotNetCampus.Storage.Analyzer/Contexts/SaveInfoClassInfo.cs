namespace DotNetCampus.Storage.Analyzer;

record SaveInfoClassInfo
{
    public required string ClassName { get; init; }
    public required string ClassFullName { get; init; }
    public required string Namespace { get; init; }
    public required string ContractName { get; init; }
    public required List<SaveInfoPropertyInfo> Properties { get; init; }
}