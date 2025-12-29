using System.Diagnostics;
using DotNetCampus.Storage.StorageFiles;

namespace DotNetCampus.Storage.Documents.StorageDocuments;

/// <summary>
/// 引用关系字典
/// </summary>
/// 专门为了查询优化。缺点是需要存三份，占用内存比较多
internal class ReferenceInfoDictionary
{
    public IReadOnlyList<ReferenceInfo> References => ReferenceInfoList;

    private List<ReferenceInfo> ReferenceInfoList { get; } = [];
    private Dictionary<StorageReferenceId, ReferenceInfo> ReferenceIdDictionary { get; } = [];
    private Dictionary<StorageFileRelativePath, ReferenceInfo> ReferenceRelativePathDictionary { get; } = [];

    public void Add(ReferenceInfo referenceInfo)
    {
        if (ReferenceIdDictionary.TryGetValue(referenceInfo.ReferenceId, out var oldValue))
        {
            if (ReferenceEquals(oldValue, referenceInfo))
            {
                return;
            }

            ReferenceInfoList.Remove(oldValue);
        }

        ReferenceInfoList.Add(referenceInfo);
        ReferenceIdDictionary[referenceInfo.ReferenceId] = referenceInfo;
        ReferenceRelativePathDictionary[referenceInfo.FilePath] = referenceInfo;

        Debug.Assert(ReferenceIdDictionary.Count == ReferenceRelativePathDictionary.Count);
        Debug.Assert(ReferenceIdDictionary.Count == ReferenceInfoList.Count);
    }

    /// <summary>
    /// 添加一组引用信息，要求不重复，且和现有的引用信息不重复
    /// </summary>
    /// <param name="references"></param>
    internal void AddRange(IReadOnlyCollection<ReferenceInfo> references)
    {
        ReferenceInfoList.AddRange(references);
        foreach (var referenceInfo in references)
        {
            ReferenceIdDictionary[referenceInfo.ReferenceId] = referenceInfo;
            ReferenceRelativePathDictionary[referenceInfo.FilePath] = referenceInfo;
        }
    }

    public void Remove(ReferenceInfo referenceInfo)
    {
        ReferenceInfoList.Remove(referenceInfo);
        ReferenceIdDictionary.Remove(referenceInfo.ReferenceId);
        ReferenceRelativePathDictionary.Remove(referenceInfo.FilePath);
    }

    public void Clear()
    {
        ReferenceInfoList.Clear();
        ReferenceIdDictionary.Clear();
        ReferenceRelativePathDictionary.Clear();
    }

    public ReferenceInfo? GetReferenceInfo(StorageReferenceId referenceId)
    {
        return ReferenceIdDictionary.GetValueOrDefault(referenceId);
    }

    public ReferenceInfo? GetReferenceInfo(StorageFileRelativePath relativePath)
    {
        return ReferenceRelativePathDictionary.GetValueOrDefault(relativePath);
    }
}