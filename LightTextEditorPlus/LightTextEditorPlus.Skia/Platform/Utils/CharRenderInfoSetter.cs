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
        Debug.Assert(charDataList.Count > 0);

        // 表示连写的字符的后续字符部分的信息。由于每个字符都应该被设置一个 CharDataInfo 信息，尽管对于连写字的后续字符来说都是相同的值，但也不得不设置一个
        CharDataInfo ligatureContinueCharDataInfo = charSizeInfoSpan[0].CharDataInfo with
        {
            FrameSize = TextSize.Zero,
            FaceSize = TextSize.Zero,
            GlyphIndex = CharDataInfo.UndefinedGlyphIndex,
            Status = CharDataInfoStatus.LigatureContinue,
        };

        Span<int> charDataUtf16StartIndexSpan = charDataList.Count + 1 <= 128
            ? stackalloc int[charDataList.Count + 1]
            : new int[charDataList.Count + 1];

        int totalUtf16Length = 0;
        for (int i = 0; i < charDataList.Count; i++)
        {
            charDataUtf16StartIndexSpan[i] = totalUtf16Length;
            totalUtf16Length += charDataList[i].CharObject.CodePoint.CharLength;
        }

        charDataUtf16StartIndexSpan[charDataList.Count] = totalUtf16Length;// 记录最后一个字符的 UTF-16 结束位置，便于后续二分查找

        Span<uint> logicalClusterSpan = charSizeInfoSpan.Length <= 128
            ? stackalloc uint[charSizeInfoSpan.Length]
            : new uint[charSizeInfoSpan.Length];

        int logicalClusterCount = 0;
        for (int i = 0; i < charSizeInfoSpan.Length; i++)
        {
            uint glyphCluster = charSizeInfoSpan[i].GlyphCluster;
            Debug.Assert(glyphCluster <= totalUtf16Length);
            logicalClusterSpan[logicalClusterCount] = glyphCluster > totalUtf16Length ? (uint) totalUtf16Length : glyphCluster;
            logicalClusterCount++;
        }

        // 排序，用于处理从右到左的字符的情况
        logicalClusterSpan = logicalClusterSpan[..logicalClusterCount];
        logicalClusterSpan.Sort();

        // 去重，正常来说用不着
        int logicalClusterUniqueCount = 0;
        for (int i = 0; i < logicalClusterSpan.Length; i++)
        {
            uint glyphCluster = logicalClusterSpan[i];
            if (logicalClusterUniqueCount == 0 || logicalClusterSpan[logicalClusterUniqueCount - 1] != glyphCluster)
            {
                logicalClusterSpan[logicalClusterUniqueCount] = glyphCluster;
                logicalClusterUniqueCount++;
            }
        }

        logicalClusterSpan = logicalClusterSpan.Slice(0, logicalClusterUniqueCount);

        for (int i = 0; i < charSizeInfoSpan.Length; i++)
        {
            CharRenderInfo charRenderInfo = charSizeInfoSpan[i];
            CharDataInfo charDataInfo = charRenderInfo.CharDataInfo;

            int currentCharDataIndex = FindCharDataIndexByUtf16Index(charDataUtf16StartIndexSpan, totalUtf16Length, charRenderInfo.GlyphCluster);
            int currentLogicalClusterIndex = FindClusterIndex(logicalClusterSpan, charRenderInfo.GlyphCluster);
            int nextCharDataIndexExclusive = currentLogicalClusterIndex == logicalClusterSpan.Length - 1
                ? charDataList.Count
                : FindCharDataIndexByUtf16Index(charDataUtf16StartIndexSpan, totalUtf16Length,
                    logicalClusterSpan[currentLogicalClusterIndex + 1]);

            CharData charData = charDataList[currentCharDataIndex];

            // 可能存在 liga 连写的情况
            if (currentCharDataIndex + 1 < nextCharDataIndexExclusive)
            {
                for (int ligatureContinueIndex = currentCharDataIndex + 1;
                     ligatureContinueIndex < nextCharDataIndexExclusive;
                     ligatureContinueIndex++)
                {
                    SetLigatureContinue(ligatureContinueIndex);
                }

                charDataInfo = charDataInfo with
                {
                    // 应当设置当前字符为连字开始
                    Status = CharDataInfoStatus.LigatureStart,
                };
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
        }

        void SetLigatureContinue(int charIndex)
        {
            CharData ligatureCharData = charDataList[charIndex];
            // 连写的字符，均设置为 LigatureContinue 状态。且无宽度高度值
            charDataLayoutInfoSetter.SetCharDataInfo(ligatureCharData, ligatureContinueCharDataInfo);
        }

        static int FindClusterIndex(ReadOnlySpan<uint> logicalClusterSpan, uint glyphCluster)
        {
            int left = 0;
            int right = logicalClusterSpan.Length - 1;

            while (left <= right)
            {
                int mid = left + ((right - left) >> 1);
                uint currentCluster = logicalClusterSpan[mid];
                if (currentCluster == glyphCluster)
                {
                    return mid;
                }

                if (currentCluster < glyphCluster)
                {
                    left = mid + 1;
                }
                else
                {
                    right = mid - 1;
                }
            }

            Debug.Fail($"无法找到 GlyphCluster={glyphCluster} 对应的逻辑 cluster。");
            return Math.Clamp(left, 0, logicalClusterSpan.Length - 1);
        }

        static int FindCharDataIndexByUtf16Index(ReadOnlySpan<int> charDataUtf16StartIndexSpan, int totalUtf16Length, uint glyphCluster)
        {
            int utf16Index = glyphCluster > totalUtf16Length ? totalUtf16Length : (int) glyphCluster;

            int left = 0;
            int right = charDataUtf16StartIndexSpan.Length - 1;
            while (left < right)
            {
                int mid = left + ((right - left + 1) >> 1);
                if (charDataUtf16StartIndexSpan[mid] <= utf16Index)
                {
                    left = mid;
                }
                else
                {
                    right = mid - 1;
                }
            }

            return Math.Min(left, charDataUtf16StartIndexSpan.Length - 2);
        }
    }
}
