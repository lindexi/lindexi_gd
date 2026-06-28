using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace LightTextEditorPlus;

/// <summary>
/// 统一管理 Native 互操作对象的 ID 分配与字典存储。
/// 负数 Id 视为错误码，直接 echo 回调用方。
/// </summary>
/// <typeparam name="T">存储的托管对象类型</typeparam>
internal class NativeObjectStore<T> where T : class
{
    private int _id = 0;

    private readonly ConcurrentDictionary<int, T> _dictionary = new();

    private readonly ErrorCode _notFoundErrorCode;
    private readonly ErrorCode _beFreeErrorCode;

    public NativeObjectStore(ErrorCode notFoundErrorCode, ErrorCode beFreeErrorCode)
    {
        _notFoundErrorCode = notFoundErrorCode;
        _beFreeErrorCode = beFreeErrorCode;
    }

    /// <summary>
    /// 创建并存储对象，返回分配的 Id（正数）。
    /// </summary>
    public int Create(T obj)
    {
        int id = Interlocked.Increment(ref _id);
        _dictionary[id] = obj;
        return id;
    }

    /// <summary>
    /// 尝试获取对象。负数 Id 直接作为错误码返回。
    /// </summary>
    public bool TryGet(int id, [NotNullWhen(true)] out T? obj, out ErrorCode errorCode)
    {
        if (id < 0)
        {
            // 负数本身就是错误码，直接 echo
            errorCode = new ErrorCode(id, "");
            obj = null;
            return false;
        }

        if (_dictionary.TryGetValue(id, out obj))
        {
            errorCode = ErrorCode.Success;
            return true;
        }

        errorCode = id < _id ? _beFreeErrorCode : _notFoundErrorCode;
        return false;
    }

    /// <summary>
    /// 尝试移除对象。负数 Id 直接作为错误码返回。
    /// </summary>
    public bool TryRemove(int id, out ErrorCode errorCode)
    {
        if (id < 0)
        {
            errorCode = new ErrorCode(id, "");
            return false;
        }

        if (_dictionary.TryRemove(id, out _))
        {
            errorCode = ErrorCode.Success;
            return true;
        }

        errorCode = id < _id ? _beFreeErrorCode : _notFoundErrorCode;
        return false;
    }

    /// <summary>
    /// 尝试更新对象。负数 Id 直接作为错误码返回。
    /// </summary>
    public int Update(int id, Func<T, T> update)
    {
        if (!TryGet(id, out var obj, out var errorCode))
        {
            return errorCode;
        }

        _dictionary[id] = update(obj);
        return ErrorCode.Success;
    }
}
