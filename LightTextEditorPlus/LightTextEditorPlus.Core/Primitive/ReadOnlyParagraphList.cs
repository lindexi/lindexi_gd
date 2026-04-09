using System;
using System.Collections;
using System.Collections.Generic;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Resources;

namespace LightTextEditorPlus.Core.Primitive;

/// <summary>
/// 当前的段落列表，一个空文本至少有一个段落
/// </summary>
public sealed class ReadOnlyParagraphList : IReadOnlyList<ITextParagraph>
{
    internal ReadOnlyParagraphList(TextEditorCore textEditor)
    {
        _textEditor = textEditor;
    }

    private readonly TextEditorCore _textEditor;

    /// <inheritdoc />
    public IEnumerator<ITextParagraph> GetEnumerator()
    {
        return new ParagraphEnumerator(this);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <inheritdoc />
    public int Count => GetParagraphList().Count;

    /// <inheritdoc />
    public ITextParagraph this[int index]
    {
        get => GetParagraphList()[index];
    }

    private IReadOnlyList<ParagraphData> GetParagraphList() =>
        _textEditor.DocumentManager.ParagraphManager.GetParagraphList();

    private sealed class ParagraphEnumerator : IEnumerator<ITextParagraph>
    {
        public ParagraphEnumerator(ReadOnlyParagraphList paragraphList)
        {
            _textEditor = paragraphList._textEditor;
            _list = paragraphList.GetParagraphList();
            _documentChangeVersion = _textEditor.DocumentManager.ChangeVersion;
        }

        private void ThrowIfChanged()
        {
            if (_documentChangeVersion != _textEditor.DocumentManager.ChangeVersion)
            {
                ThrowEnumFailedVersionException();
            }
        }

        public bool MoveNext()
        {
            ThrowIfChanged();

            _index++;
            if (_index == _list.Count)
            {
                return false;
            }

            return true;
        }

        public void Reset()
        {
            ThrowIfChanged();

            _index = -1;
        }

        public void Dispose()
        {
        }

        private static void ThrowEnumFailedVersionException()
        {
            throw new InvalidOperationException(ExceptionMessages.Get(nameof(ReadOnlyParagraphList) + "_EnumFailedVersion"));
        }

        private int _index = -1;
        private readonly IReadOnlyList<ParagraphData> _list;
        private readonly TextEditorCore _textEditor;
        private readonly int _documentChangeVersion;

        public ITextParagraph Current
        {
            get
            {
                ThrowIfChanged();

                if (_index == -1 || (_index == _list.Count))
                {
                    throw new InvalidOperationException("InvalidOperation_EnumOpCantHappen");
                }

                return _list[_index];
            }
        }

        object? IEnumerator.Current => Current;
    }
}