using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkiaInkCore.Utils;
internal class InkingFixedQueue<T> : ICollection, IReadOnlyList<T>
{
    private readonly T[] _array;
    private int _start = 0;
    private int _length = 0;

    private readonly List<T>? _historyDequeueList;

    /// <summary>
    /// 创建带最大数量的队列
    /// </summary>
    /// <param name="maxCount"></param>
    /// <param name="historyDequeueList">历史输出的元素的列表，每次 <see cref="Dequeue()"/> 时将会自动加入此列表中</param>
    public InkingFixedQueue(int maxCount, List<T>? historyDequeueList = null)
    {
        _array = new T[maxCount];
        _historyDequeueList = historyDequeueList;
    }

    /// <summary>
    /// 队列可以使用的最大元素数量
    /// </summary>
    public int MaxCount => _array.Length;

    #region Queue相关的成员

    class FixedQueueEnumerator<TEnumerator> : IEnumerator<TEnumerator>
    {
        public FixedQueueEnumerator(InkingFixedQueue<TEnumerator> queue)
        {
            _queue = queue;
            Current = default!;
        }

        private readonly InkingFixedQueue<TEnumerator> _queue;
        private int _index = -1;
        public bool MoveNext()
        {
            _index++;
            if (_index < _queue.Count)
            {
                Current = _queue[_index];
                return true;
            }
            return false;
        }

        public void Reset()
        {
            _index = -1;
            Current = default!;
        }

        public TEnumerator Current { get; private set; }
        object? IEnumerator.Current => Current;
        public void Dispose()
        {
        }
    }

    /// <summary>
    /// Gets the enumerator.
    /// </summary>
    /// <returns></returns>
    public IEnumerator<T> GetEnumerator()
    {
        return new FixedQueueEnumerator<T>(this);
    }

    /// <summary>
    /// 返回一个循环访问集合的枚举器。
    /// </summary>
    /// <returns>
    /// 可用于循环访问集合的 <see cref="T:System.Collections.IEnumerator" /> 对象。
    /// </returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>
    /// 从特定的 <see cref="T:System.Array" /> 索引处开始，将 <see cref="T:System.Collections.ICollection" /> 的元素复制到一个 <see cref="T:System.Array" /> 中。
    /// </summary>
    /// <param name="array">作为从 <see cref="T:System.Collections.ICollection" /> 复制的元素的目标位置的一维 <see cref="T:System.Array" />。<see cref="T:System.Array" /> 必须具有从零开始的索引。</param>
    /// <param name="index"><paramref name="array" /> 中从零开始的索引，将在此处开始复制。</param>
    public void CopyTo(Array array, int index) => throw new NotSupportedException();

    /// <summary>
    /// Copies to.
    /// </summary>
    /// <param name="array">The array.</param>
    /// <param name="index">The index.</param>
    public void CopyTo(T[] array, int index) => throw new NotSupportedException();

    /// <summary>
    /// 获取 <see cref="T:System.Collections.ICollection" /> 中包含的元素数。
    /// </summary>
    /// <returns>
    ///   <see cref="T:System.Collections.ICollection" /> 中包含的元素数。</returns>
    public int Count => _length;

    /// <summary>
    /// 获取一个可用于同步对 <see cref="T:System.Collections.ICollection" /> 的访问的对象。
    /// </summary>
    /// <returns>可用于同步对 <see cref="T:System.Collections.ICollection" /> 的访问的对象。</returns>
    public object SyncRoot => _array.SyncRoot;

    /// <summary>
    /// 获取一个值，该值指示是否同步对 <see cref="T:System.Collections.ICollection" /> 的访问（线程安全）。
    /// </summary>
    /// <returns>如果对 <see cref="T:System.Collections.ICollection" /> 的访问是同步的（线程安全），则为 true；否则为 false。</returns>
    public bool IsSynchronized => false;// 因为包装不是线程安全的，如 Enqueue 等方法，所以整个类是线程不安全的
    #endregion

    /// <summary>
    /// 将对象添加到队列结尾处
    /// </summary>
    /// <param name="item"></param>
    public void Enqueue(T item)
    {
        if (_length > MaxCount)
        {
            throw new InvalidOperationException("集合中的元素已超过最大限定值。");
        }
        if (_length == MaxCount)
        {
            Dequeue();
        }

        _array[(_start + _length) % _array.Length] = item;
        ++_length;
    }

    /// <summary>
    /// 移除并返回队列开始处元素
    /// </summary>
    /// <param name="item">一个没有被使用的元素，请随意传入，这是设计问题，但为了兼容性，暂时保存</param>
    /// <returns></returns>
    [Obsolete("请使用不带参数的 Dequeue 方法代替，这个方法传入的参数没有被使用")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public T Dequeue(T item)
    {
        return Dequeue();
    }

    /// <summary>
    /// 移除并返回队列开始处元素
    /// </summary>
    public T Dequeue()
    {
        var result = _array[_start];
        _start = (_start + 1) % _array.Length;
        _length--;

        _historyDequeueList?.Add(result);
        return result;
    }

    /// <summary>
    /// 返回队列开始处的对象但不将这个对象移除
    /// </summary>
    /// <returns></returns>
    public T Peek()
    {
        var result = _array[_start];
        return result;
    }

    /// <summary>
    /// 确定某元素是否在队列存在
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public bool Contains(T item)
    {
        for (int i = 0; i < Count; i++)
        {
            if (Equals(this[i], item))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 清空队列
    /// </summary>
    public void Clear()
    {
        _start = 0;
        _length = 0;
        Array.Clear(_array);
    }

    public T this[int index]
    {
        get => _array[(_start + index) % _array.Length];
        set => _array[(_start + index) % _array.Length] = value;
    }

    public T this[Index index]
    {
        get => _array[(_start + index.GetOffset(_length)) % _array.Length];
        set => _array[(_start + index.GetOffset(_length)) % _array.Length] = value;
    }
}
