using DotNetCampus.Storage.Parsers.Contexts;
using DotNetCampus.Storage.StorageNodes;

namespace DotNetCampus.Storage.Parsers;

abstract class AsyncPostNodeParser<T> : NodeParser<T>, IAsyncPostNodeParser
{
    // 先不开放给外部调用
    public Task<StorageNode> PostDeparseAsync(object obj, StorageNode oldStorageNode, DeparseNodeContext context)
    {
        if (obj is not T t)
        {
            throw new ArgumentException($"对象类型错误，期望类型：{typeof(T).FullName}，实际类型：{obj.GetType().FullName}");
        }

        return PostDeparseCoreAsync(t, oldStorageNode, context);
    }

    protected abstract Task<StorageNode> PostDeparseCoreAsync(T obj, StorageNode oldStorageNode, DeparseNodeContext context);
}