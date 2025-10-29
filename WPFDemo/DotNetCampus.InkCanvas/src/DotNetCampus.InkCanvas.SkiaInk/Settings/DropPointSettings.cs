using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetCampus.Inking.Settings;

/// <summary>
/// 丢点配置
/// </summary>
public record DropPointSettings
{
    /// <summary>
    /// 最大丢点数量
    /// </summary>
    public int MaxDropPointCount { get; init; } = 10;

    public int MaxDistanceLength { get; init; } = 2;

    /// <summary>
    /// 丢点策略
    /// </summary>
    public DropPointStrategy DropPointStrategy { get; init; } = DropPointStrategy.Normal;
}

/// <summary>
/// 丢点策略
/// </summary>
public enum DropPointStrategy
{
    /// <summary>
    /// 普通的丢点，不会丢太多
    /// </summary>
    Normal,

    /// <summary>
    /// 激进策略，会丢很多点
    /// </summary>
    Aggressive,
}
