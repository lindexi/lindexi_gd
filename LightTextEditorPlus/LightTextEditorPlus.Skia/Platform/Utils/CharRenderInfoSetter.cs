using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Layout;
using LightTextEditorPlus.Core.Layout.LayoutUtils;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.Primitive.Collections;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightTextEditorPlus.Platform.Utils;

/// <summary>
/// 用于将字符信息设置到 CharData 中，CharRenderInfo 是用于渲染的字符信息，包含了字符的大小、位置等信息
/// </summary>
/// <param name="Setter"></param>
/// 只是因为设置的逻辑比较复杂，所以单独抽离出来一个类来处理，主要处理情况：
/// - StandardLigatures 'liga' 连写的情况，根据 GlyphCluster 数据决定哪些字符属于连写，需要跳过的情况
/// - 从右到左的字符处理
/// - emoji 表情等特殊字符的处理
internal readonly record struct CharRenderInfoSetter(ICharDataLayoutInfoSetter Setter)
{
    /// <summary>
    /// 设置字符信息
    /// </summary>
    /// <param name="charSizeInfoSpan"></param>
    /// <param name="charDataList"></param>
    /// 设置的时候的一个重点是处理 StandardLigatures 'liga' 连写的情况，根据 GlyphCluster 数据决定哪些字符属于连写，需要跳过的情况
    public void SetCharDataInfo
    (
        Span<CharRenderInfo> charSizeInfoSpan,
        TextReadOnlyListSpan<CharData> charDataList
    )
    {
        var charDataLayoutInfoSetter = Setter;
        Debug.Assert(charSizeInfoSpan.Length > 0);

        // 表示连写的字符的后续字符部分的信息。由于每个字符都应该被设置一个 CharDataInfo 信息，尽管对于连写字的后续字符来说都是相同的值，但也不得不设置一个
        CharDataInfo ligatureContinueCharDataInfo = charSizeInfoSpan[0].CharDataInfo with
        {
            FrameSize = TextSize.Zero,
            FaceSize = TextSize.Zero,
            GlyphIndex = CharDataInfo.UndefinedGlyphIndex,
            Status = CharDataInfoStatus.LigatureContinue,
        };

        // charDataListIndex 为什么从 0 开始，而不是 argument.CurrentIndex 开始？原因是在 runList 里面已经使用 Slice 裁剪了
        // 为什么有 i 还不够，还需要 charDataListIndex 变量？原因是存在 StandardLigatures （liga） 连写的情况，可能实际的 glyph 数量会小于字符数量，这是符合预期的
        int charDataListIndex = 0;
        for (int i = 0; i < charSizeInfoSpan.Length; i++)
        {
            CharRenderInfo charRenderInfo = charSizeInfoSpan[i];
            CharDataInfo charDataInfo = charRenderInfo.CharDataInfo;

            CharData charData = charDataList[charDataListIndex];

            // 可能存在 liga 连写的情况
            if (i != charSizeInfoSpan.Length - 1)
            {
                CharRenderInfo nextInfo = charSizeInfoSpan[i + 1];
                if (nextInfo.GlyphCluster > charDataListIndex + 1)
                {
                    // 证明下一个字形的 cluster 是超过当前字符的下一个字符的
                    // 说明当前字符是 liga 连写的开始
                    for (charDataListIndex = charDataListIndex + 1;
                         charDataListIndex < nextInfo.GlyphCluster;
                         charDataListIndex++)
                    {
                        SetLigatureContinue(charDataListIndex);
                    }

                    // 因为循环自带加一的效果，需要冲掉
                    charDataListIndex--;

                    charDataInfo = charDataInfo with
                    {
                        // 应当设置当前字符为连字开始
                        Status = CharDataInfoStatus.LigatureStart,
                    };
                }
            }
            else
            {
                // 最后一个字符了，如果末尾没有了，则证明最后一个是连字符
                if (charDataListIndex + 1 < charDataList.Count)
                {
                    for (charDataListIndex = charDataListIndex + 1;
                         charDataListIndex < charDataList.Count;
                         charDataListIndex++)
                    {
                        SetLigatureContinue(charDataListIndex);
                    }

                    // 因为循环自带加一的效果，需要冲掉。虽然这是多余的，因为立刻就退出循环了
                    charDataListIndex--;

                    charDataInfo = charDataInfo with
                    {
                        // 应当设置当前字符为连字开始
                        Status = CharDataInfoStatus.LigatureStart,
                    };
                }
            }

            if (charData.IsInvalidCharDataInfo)
            {
                charDataLayoutInfoSetter.SetCharDataInfo(charData, charDataInfo);
            }
            else if (charData.CharDataInfo.Status != charDataInfo.Status)
            {
                // 如果是属于连写的开始，则需要更新状态。比如说有 'fi' 连写。分为两次输入，第一次输入 'f'，第二次输入 'i'。第一次输入 'f' 时，可能已经测量过了，并且状态是 Normal 值。此时当输入 'i' 时，才发现 'fi' 是连写的开始，则需要更新 'f' 的状态为 LigatureStart 值
                charDataLayoutInfoSetter.SetCharDataInfo(charData, charDataInfo);
            }
            else if (charData.CharDataInfo.GlyphIndex != charDataInfo.GlyphIndex)
            {
                // 比如蒙古文的情况。蒙古文需要根据后续字符来决定前续字符的字形索引，此时符合预期
                charDataLayoutInfoSetter.SetCharDataInfo(charData, charDataInfo);
            }

            charDataListIndex++;
        }

        void SetLigatureContinue(int charIndex)
        {
            CharData ligatureCharData = charDataList[charIndex];
            // 连写的字符，均设置为 LigatureContinue 状态。且无宽度高度值
            charDataLayoutInfoSetter.SetCharDataInfo(ligatureCharData, ligatureContinueCharDataInfo);
        }
    }
}
