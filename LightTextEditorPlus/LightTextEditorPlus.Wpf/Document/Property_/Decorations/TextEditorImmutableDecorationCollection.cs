using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Windows;

namespace LightTextEditorPlus.Document.Decorations;

/// <summary>
/// 文本的装饰不可变集合
/// </summary>
/// 其作用是放在 RunProperty 里面时，可以不用一定开启一个新的对象。绝大部分时候，只添加一个装饰时，只开一个对象，也不用开两个对象（一个 List 一个元素），可以更省一些
public readonly struct TextEditorImmutableDecorationCollection : IEquatable<TextEditorImmutableDecorationCollection>
{
    /// <summary>
    /// 创建文本的装饰不可变集合
    /// </summary>
    public TextEditorImmutableDecorationCollection(TextEditorDecoration decoration)
    {
        _one = decoration;
    }

    /// <summary>
    /// 创建文本的装饰不可变集合
    /// </summary>
    /// <param name="decorationCollection"></param>
    public TextEditorImmutableDecorationCollection(ICollection<TextEditorDecoration> decorationCollection)
    {
        if (decorationCollection.Count == 1)
        {
            _one = decorationCollection.First();
        }
        else if (decorationCollection.Count > 1)
        {
            _more = decorationCollection.ToImmutableList();
        }
    }

    /// <summary>
    /// 创建文本的装饰不可变集合
    /// </summary>
    /// <param name="decorationImmutableList"></param>
    public TextEditorImmutableDecorationCollection(ImmutableList<TextEditorDecoration> decorationImmutableList)
    {
        if (decorationImmutableList.Count > 0)
        {
            if (decorationImmutableList.Count == 1)
            {
                _one = decorationImmutableList[0];
            }
            else
            {
                _more = decorationImmutableList;
            }
        }
    }

    /// <summary>
    /// 获取指定索引的装饰
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public TextEditorDecoration this[int index]
    {
        get
        {
            if (index >= Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), $"Count={Count};Index={index}");
            }

            if (index < 0)
            {
                throw new ArgumentException($"传入的索引不能小于 0 的值", nameof(index));
            }

            if (index == 0 && _one is not null)
            {
                return _one;
            }
            else
            {
                return _more![index];
            }
        }
    }

    /// <summary>
    /// 包含的数量
    /// </summary>
    public int Count
    {
        get
        {
            if (_one is null && _more is null)
            {
                return 0;
            }

            if (_more != null)
            {
                return _more.Count;
            }

            return _one is not null ? 1 : 0;
        }
    }

    private readonly ImmutableList<TextEditorDecoration>? _more;
    private readonly TextEditorDecoration? _one;

    /// <summary>
    /// 添加一个装饰到不可变集合中，自带去重功能
    /// </summary>
    /// <param name="decoration"></param>
    /// <returns></returns>
    public TextEditorImmutableDecorationCollection Add(TextEditorDecoration decoration)
    {
        if (_one is null && _more is null)
        {
            return new TextEditorImmutableDecorationCollection(decoration);
        }
        else if (_one is not null && _more is null)
        {
            if (_one.Equals(decoration))
            {
                return this;
            }

            return new TextEditorImmutableDecorationCollection(ImmutableList.Create(_one, decoration));
        }
        else
        {
            // _more is not null
            if (_more is null)
            {
                // 这不可能发生
                return this;
            }

            if (_more.Contains(decoration))
            {
                return this;
            }

            return new TextEditorImmutableDecorationCollection(_more.Add(decoration));
        }
    }

    /// <summary>
    /// 添加一组装饰到不可变集合中，无去重
    /// </summary>
    /// <param name="decorations"></param>
    /// <returns></returns>
    internal TextEditorImmutableDecorationCollection AddRange(ICollection<TextEditorDecoration> decorations)
    {
        if (_one is null && _more is null)
        {
            return new TextEditorImmutableDecorationCollection(decorations);
        }
        else if (_one is not null && _more is null)
        {
            ImmutableList<TextEditorDecoration>.Builder builder = ImmutableList.CreateBuilder<TextEditorDecoration>();
            builder.Add(_one);
            builder.AddRange(decorations);

            ImmutableList<TextEditorDecoration> immutableList = builder.ToImmutable();

            return new TextEditorImmutableDecorationCollection(immutableList);
        }
        else
        {
            // _more is not null
            if (_more is null)
            {
                // 这不可能发生
                return this;
            }

            return new TextEditorImmutableDecorationCollection(_more.AddRange(decorations));
        }
    }

    /// <summary>
    /// 添加一个不可变集合到当前不可变集合中
    /// </summary>
    /// <param name="collection"></param>
    /// <returns></returns>
    public TextEditorImmutableDecorationCollection Add(in TextEditorImmutableDecorationCollection collection)
    {
        int collectionCount = collection.Count;
        if (collectionCount == 0)
        {
            return this;
        }

        if (_one is null && _more is null)
        {
            return collection;
        }
        else if (_one is not null && _more is null)
        {
            if (collectionCount == 1)
            {
                if (_one.Equals(collection._one))
                {
                    return this;
                }

                return new TextEditorImmutableDecorationCollection(ImmutableList.Create(_one, collection._one!));
            }
            else
            {
                Debug.Assert(collection._more != null, "由于 collectionCount 大于 1 因此 _more 必定不是空");
                if (collection._more.Contains(_one))
                {
                    return collection;
                }

                ImmutableList<TextEditorDecoration>.Builder builder =
                    ImmutableList.CreateBuilder<TextEditorDecoration>();
                builder.Add(_one);
                builder.AddRange(collection._more!);
                return new TextEditorImmutableDecorationCollection(builder.ToImmutable());
            }
        }
        else
        {
            // _more is not null
            if (_more is null)
            {
                // 这不可能发生
                return this;
            }

            if (collectionCount == 1)
            {
                if (_more.Contains(collection._one!))
                {
                    return this;
                }

                return new TextEditorImmutableDecorationCollection(_more.Add(collection._one!));
            }
            else
            {
                ImmutableList<TextEditorDecoration>.Builder builder = _more.ToBuilder();

                foreach (TextEditorDecoration textEditorDecoration in collection._more!)
                {
                    if (!builder.Contains(textEditorDecoration))
                    {
                        builder.Add(textEditorDecoration);
                    }
                }

                ImmutableList<TextEditorDecoration> immutableList = builder.ToImmutable();
                return new TextEditorImmutableDecorationCollection(immutableList);
            }
        }
    }

    /// <summary>
    /// 从不可变集合中移除一个装饰
    /// </summary>
    /// <param name="decoration"></param>
    /// <returns></returns>
    public TextEditorImmutableDecorationCollection Remove(TextEditorDecoration? decoration)
    {
        if (decoration is null)
        {
            return this;
        }

        if (_one is not null)
        {
            if (_one.Equals(decoration))
            {
                return new TextEditorImmutableDecorationCollection();
            }
            else
            {
                return this;
            }
        }
        else
        {
            Debug.Assert(_one is null);
            if (_more is not null)
            {
                ImmutableList<TextEditorDecoration> newMore = _more.Remove(decoration);
                if (newMore.Count == 0)
                {
                    return new TextEditorImmutableDecorationCollection();
                }
                else if (newMore.Count == 1)
                {
                    return new TextEditorImmutableDecorationCollection(newMore[0]);
                }
                else
                {
                    return new TextEditorImmutableDecorationCollection(newMore);
                }
            }
            else
            {
                return this;
            }
        }
    }

    /// <inheritdoc />
    public bool Equals(TextEditorImmutableDecorationCollection other)
    {
        return Equals(_more, other._more) && Equals(_one, other._one);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is TextEditorImmutableDecorationCollection other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(_more, _one);
    }

    /// <summary>
    /// 隐式转换
    /// </summary>
    /// <param name="decoration"></param>
    public static implicit operator TextEditorImmutableDecorationCollection(TextEditorDecoration decoration)
    {
        return new TextEditorImmutableDecorationCollection(decoration);
    }

    /// <summary>
    /// 包含指定的装饰
    /// </summary>
    /// <param name="textDecoration"></param>
    /// <returns></returns>
    public bool Contains(TextEditorDecoration textDecoration)
    {
        if (_one is null && _more is null)
        {
            return false;
        }
        else if (_one is not null)
        {
            return _one.Equals(textDecoration);
        }
        else
        {
            Debug.Assert(_more != null);
            return _more.Contains(textDecoration);
        }
    }
}