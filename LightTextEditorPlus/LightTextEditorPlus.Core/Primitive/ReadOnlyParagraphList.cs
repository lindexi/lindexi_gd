using System;
using System.Collections;
using System.Collections.Generic;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Document.DocumentEventArgs;

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
            _textEditor.DocumentManager.InternalDocumentChanged += DocumentManager_InternalDocumentChanged;
            _list = paragraphList.GetParagraphList();
        }

        private bool _isDirty = false;

        private void DocumentManager_InternalDocumentChanged(object? sender, DocumentChangeEventArgs e)
        {
            _isDirty = true;

            _textEditor.DocumentManager.InternalDocumentChanged -= DocumentManager_InternalDocumentChanged;
        }

        public bool MoveNext()
        {
            if (_isDirty)
            {
                ThrowEnumFailedVersionException();
            }

            _index++;
            if (_index == _list.Count)
            {
                return false;
            }

            return true;
        }

        public void Reset()
        {
            if (_isDirty)
            {
                ThrowEnumFailedVersionException();
            }

            _index = -1;
        }

        public void Dispose()
        {
            _textEditor.DocumentManager.InternalDocumentChanged -= DocumentManager_InternalDocumentChanged;
        }

        private static void ThrowEnumFailedVersionException()
        {
            throw new InvalidOperationException($"枚举过程中，文本已经变更，不再可遍历");
        }

        private int _index = -1;
        private readonly IReadOnlyList<ParagraphData> _list;
        private readonly TextEditorCore _textEditor;

        public ITextParagraph Current
        {
            get
            {
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