using System.Collections.Generic;
using LightTextEditorPlus.Core.Document;
using System.Text;
using LightTextEditorPlus.Core.Carets;

namespace LightTextEditorPlus.Core;

/// <summary>
/// 文本编辑扩展方法
/// </summary>
public static class TextEditorCoreTextExtensions
{
    /// <summary>
    /// 获取整个文本编辑器的文本
    /// </summary>
    /// <param name="textEditor"></param>
    /// <returns></returns>
    public static string GetText(this TextEditorCore textEditor)
    {
        return GetText(textEditor, textEditor.DocumentManager.GetAllDocumentSelection());
    }

    /// <summary>
    /// 获取文本
    /// </summary>
    /// <param name="textEditor"></param>
    /// <param name="selection"></param>
    /// <returns></returns>
    public static string GetText(this TextEditorCore textEditor, in Selection selection)
    {
        return GetText(textEditor, new StringBuilder(), in selection).ToString();
    }

    /// <summary>
    /// 获取整个文本编辑器的文本
    /// </summary>
    public static StringBuilder GetText(this TextEditorCore textEditor, StringBuilder stringBuilder) => GetText(textEditor, stringBuilder, textEditor.DocumentManager.GetAllDocumentSelection());

    /// <summary>
    /// 获取文本
    /// </summary>
    public static StringBuilder GetText(this TextEditorCore textEditor, StringBuilder stringBuilder,
        in Selection selection)
    {
        foreach (CharData charData in textEditor.DocumentManager.GetCharDataRange(in selection))
        {
            charData.CharObject.CodePoint.AppendToStringBuilder(stringBuilder);
        }

        return stringBuilder;
    }

    /// <summary>
    /// 获取富文本
    /// </summary>
    /// <param name="textEditor"></param>
    /// <param name="selection"></param>
    /// <returns></returns>
    public static IImmutableRunList GetRunList(this TextEditorCore textEditor, in Selection selection)
    {
        MutableRunList runList = new MutableRunList();

        CharObjectTextRun? currentRun = null;
        foreach (CharData charData in textEditor.DocumentManager.GetCharDataRange(in selection))
        {
            if (currentRun is null)
            {
                currentRun = new CharObjectTextRun(charData.RunProperty)
                {
                    charData.CharObject
                };
                runList.Add(currentRun);
            }
            else
            {
                if (!charData.RunProperty.Equals(currentRun.RunProperty))
                {
                    currentRun = new CharObjectTextRun(charData.RunProperty)
                    {
                        charData.CharObject
                    };
                    runList.Add(currentRun);
                }
                else
                {
                    currentRun.Add(charData.CharObject);
                }
            }
        }

        return runList;
    }

    class CharObjectTextRun : List<ICharObject>, IImmutableRun
    {
        public CharObjectTextRun(IReadOnlyRunProperty runProperty)
        {
            RunProperty = runProperty;
        }

        public CharObjectTextRun(IEnumerable<ICharObject> collection, IReadOnlyRunProperty runProperty) : base(collection)
        {
            RunProperty = runProperty;
        }

        public ICharObject GetChar(int index)
        {
            return this[index];
        }

        public IReadOnlyRunProperty RunProperty { get; }
        public (IImmutableRun FirstRun, IImmutableRun SecondRun) SplitAt(int index)
        {
            List<ICharObject> first = GetRange(0,index);
            List<ICharObject> second = GetRange(index,Count - index);
            return (new CharObjectTextRun(first, RunProperty), new CharObjectTextRun(second, RunProperty));
        }
    }
}