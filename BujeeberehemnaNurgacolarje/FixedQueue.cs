using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace BujeeberehemnaNurgacolarje;

/// <summary>
/// 带最大数量的队列，超过最大数量将会自动将队头元素出队丢弃
/// </summary>
/// <typeparam name="T"></typeparam>
public class FixedQueue<T> : ICollection, IEnumerable<T>
{
    private readonly Queue<T> _innerQueue = new Queue<T>();

    /// <summary>
    /// 创建带最大数量的队列
    /// </summary>
    /// <param name="maxCount"></param>
    public FixedQueue(int maxCount)
    {
        MaxCount = maxCount;
    }

    /// <summary>
    /// 队列可以使用的最大元素数量
    /// </summary>
    public int MaxCount { get; private set; }

    #region Queue相关的成员
    /// <summary>
    /// Gets the enumerator.
    /// </summary>
    /// <returns></returns>
    public IEnumerator<T> GetEnumerator()
    {
        return _innerQueue.GetEnumerator();
    }

    /// <summary>
    /// 返回一个循环访问集合的枚举器。
    /// </summary>
    /// <returns>
    /// 可用于循环访问集合的 <see cref="T:System.Collections.IEnumerator" /> 对象。
    /// </returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return _innerQueue.GetEnumerator();
    }

    /// <summary>
    /// 从特定的 <see cref="T:System.Array" /> 索引处开始，将 <see cref="T:System.Collections.ICollection" /> 的元素复制到一个 <see cref="T:System.Array" /> 中。
    /// </summary>
    /// <param name="array">作为从 <see cref="T:System.Collections.ICollection" /> 复制的元素的目标位置的一维 <see cref="T:System.Array" />。<see cref="T:System.Array" /> 必须具有从零开始的索引。</param>
    /// <param name="index"><paramref name="array" /> 中从零开始的索引，将在此处开始复制。</param>
    public void CopyTo(Array array, int index)
    {
        ((ICollection) _innerQueue).CopyTo(array, index);
    }

    /// <summary>
    /// Copies to.
    /// </summary>
    /// <param name="array">The array.</param>
    /// <param name="index">The index.</param>
    public void CopyTo(T[] array, int index)
    {
        _innerQueue.CopyTo(array, index);
    }

    /// <summary>
    /// 获取 <see cref="T:System.Collections.ICollection" /> 中包含的元素数。
    /// </summary>
    /// <returns>
    ///   <see cref="T:System.Collections.ICollection" /> 中包含的元素数。</returns>
    public int Count { get { return _innerQueue.Count; } }
    /// <summary>
    /// 获取一个可用于同步对 <see cref="T:System.Collections.ICollection" /> 的访问的对象。
    /// </summary>
    /// <returns>可用于同步对 <see cref="T:System.Collections.ICollection" /> 的访问的对象。</returns>
    public object SyncRoot { get { return ((ICollection) _innerQueue).SyncRoot; } }

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
        if (_innerQueue.Count > MaxCount)
        {
            throw new InvalidOperationException("集合中的元素已超过最大限定值。");
        }
        if (_innerQueue.Count == MaxCount)
        {
            _innerQueue.Dequeue();
        }
        _innerQueue.Enqueue(item);
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
        return _innerQueue.Dequeue();
    }

    /// <summary>
    /// 移除并返回队列开始处元素
    /// </summary>
    public T Dequeue() => _innerQueue.Dequeue();

    /// <summary>
    /// 返回队列开始处的对象但不将这个对象移除
    /// </summary>
    /// <returns></returns>
    public T Peek()
    {
        return _innerQueue.Peek();
    }

    /// <summary>
    /// 确定某元素是否在队列存在
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public bool Contains(T item)
    {
        return _innerQueue.Contains(item);
    }

    /// <summary>
    /// 清空队列
    /// </summary>
    public void Clear()
    {
        _innerQueue.Clear();
    }
}