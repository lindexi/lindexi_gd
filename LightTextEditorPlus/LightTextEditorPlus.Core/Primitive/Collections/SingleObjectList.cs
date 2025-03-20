using System.Collections;
using System.Collections.Generic;

namespace LightTextEditorPlus.Core.Primitive.Collections;

/// <summary>
/// 只包含单个元素的列表
/// </summary>
/// 这里面的实现是不安全的，仅在框架内使用
/// <typeparam name="T"></typeparam>
internal class SingleObjectList<T> : IReadOnlyList<T>
{
    public SingleObjectList(T currentObject)
    {
        CurrentObject = currentObject;
    }

    public T CurrentObject
    {
        get;
        // 框架内没有考虑 GetEnumerator 过程中修改 CurrentObject 的情况
        set;
    }

    public IEnumerator<T> GetEnumerator()
    {
        return new SingleObjectListEnumerator(CurrentObject);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public int Count => 1;

    public T this[int index]
    {
        get => CurrentObject;
    }

    class SingleObjectListEnumerator : IEnumerator<T>
    {
        public SingleObjectListEnumerator(T current)
        {
            Current = current;
        }

        private int _count = 0;

        public bool MoveNext()
        {
            if (_count == 0)
            {
                _count++;
                return true;
            }
            return false;
        }

        public void Reset()
        {
            _count = 0;
        }

        public void Dispose()
        {
            // 啥都不用干
        }

        public T Current { get; }
        object? IEnumerator.Current => Current;
    }
}
