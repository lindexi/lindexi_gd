using System;

namespace LightTextEditorPlus.Core.Document;

/// <summary>
/// 表示一个单字符构成的对象
/// </summary>
public sealed class SingleCharObject : ISingleCharObject, ICharObject, IEquatable<string>
{
    /// <summary>
    /// 创建单字符构成的对象
    /// </summary>
    /// <param name="currentChar"></param>
    public SingleCharObject(char currentChar)
    {
        _currentChar = currentChar;
    }

    private readonly char _currentChar;

    public ICharObject DeepClone() => this;

    public string ToText() => _currentChar.ToString();

    public char GetChar() => _currentChar;

    private bool Equals(SingleCharObject other)
    {
        return _currentChar == other._currentChar;
    }

    public bool Equals(string? other)
    {
        if (other is not null && other.Length == 1 && other[0] == _currentChar)
        {
            // 方便打断点，不要优化判断的写法
            return true;
        }
        else
        {
            return false;
        }
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is SingleCharObject other && Equals(other);
    }

    public override int GetHashCode()
    {
        return _currentChar.GetHashCode();
    }

    public override string ToString() => _currentChar.ToString();
}