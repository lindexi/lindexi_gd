namespace DotNetCampus.Installer.Lib.EnvironmentCheckers;

/// <summary>
/// 环境检测结果类型
/// </summary>
public enum EnvironmentCheckResultType
{
    /// <summary>
    /// 检测通过
    /// </summary>
    Passed,

    /// <summary>
    /// 系统太旧了，无法继续安装。不需要挣扎
    /// </summary>
    FailedWithObsoleteOs,

    /// <summary>
    /// 缺少补丁，还能挣扎一下，带上 KB2533623 补丁安装再重试
    /// </summary>
    FailedWithMissingPatch,

    /// <summary>
    /// 未知的错误
    /// </summary>
    FailedWithUnknownError,
}