namespace DotNetCampus.Storage.Analyzer;

record SaveInfoNodeParserGeneratorConfigOption
{
    /// <summary>
    /// 是否应该在当前程序集内生成转换器
    /// </summary>
    public required bool ShouldGenerateSaveInfoNodeParser { get; init; }

    /// <summary>
    /// 根命名空间
    /// </summary>
    public required string? RootNamespace { get; init; }
}