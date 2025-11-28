using DotNetCampus.Storage.Parsers.Contexts;
using DotNetCampus.Storage.StorageNodes;

namespace DotNetCampus.Storage.Parsers;

interface IAsyncPostNodeParser
{
    /// <summary>
    /// 在全部转换完成后调用的异步方法，可以执行一些异步处理，比如文件哈希计算等
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="oldStorageNode"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    Task<StorageNode> PostDeparseAsync(object obj, StorageNode oldStorageNode, DeparseNodeContext context);
}