using DotNetCampus.Storage.StorageNodes;

namespace DotNetCampus.Storage.Logging;

/// <summary>
/// 复合文档的存储日志记录器
/// </summary>
public class CompoundStorageDocumentLogger
{
    // 这个应该是接口的，这里为了简化，就不写接口了

    public CrumbsStoragePathNavigator Navigator { get; } = new CrumbsStoragePathNavigator();

    public void RecordStorageNodeMissChildren(string saveInfoName, StorageNode node)
    {
        // 通过面包屑导航，可以知道是哪个节点缺少了子节点
        // 如果没有导航，只好人工去看几千行的 XML 找到错误点，比较有难度
        // 期望通过面包屑导航，可以直接定位到错误的节点
        var message = $"StorageNode {saveInfoName} miss children; Path={Navigator.GetCurrentPath()}";
        _ = message; // 现在还不知道要记录到哪去
    }
}