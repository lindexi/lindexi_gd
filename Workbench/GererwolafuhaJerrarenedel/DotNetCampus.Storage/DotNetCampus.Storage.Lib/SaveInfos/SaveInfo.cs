using DotNetCampus.Storage.StorageNodes;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetCampus.Storage.SaveInfos;

/// <summary>
/// 数据模型的基类
/// </summary>
public abstract class SaveInfo
{
    /// <summary>
    /// 未识别的属性
    /// <para>一般是新版本的 SaveInfo 在旧版本打开，其部分新增属性无法识别。</para>
    /// </summary>
    public IReadOnlyList<StorageNode>? UnknownProperties { get; internal set; }

    private List<SaveInfo>? _extensions;

    /// <summary>
    /// 获取或设置 SaveInfo 的扩展数据。
    /// </summary>
    public List<SaveInfo> Extensions
    {
        get => _extensions ??= new List<SaveInfo>();
        set => _extensions = value;
    }
}
