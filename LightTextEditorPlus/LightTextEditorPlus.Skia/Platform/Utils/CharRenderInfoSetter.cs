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

        // HarfBuzz 返回的 GlyphCluster 是基于 UTF-16 输入的索引，而不是 CharData 索引。
        // 因此这里要先建立「CharData 索引 -> UTF-16 起始位置」的映射关系。
        // 额外多开的最后一个槽位不是字符，只是末尾哨兵，记录整个 run 的 UTF-16 总长度。
        // 有了这个哨兵之后，后续可以统一用 [当前起点, 下一个起点) 的方式描述字符区间，
        // 同时二分查找时也可以安全地把“落在末尾之后”的位置夹到最后一个真实字符上。
        Span<int> charDataUtf16StartIndexSpan = charDataList.Count + 1 <= 128
            ? stackalloc int[charDataList.Count + 1]
            : new int[charDataList.Count + 1];

        int totalUtf16Length = 0;
        for (int i = 0; i < charDataList.Count; i++)
        {
            charDataUtf16StartIndexSpan[i] = totalUtf16Length;
            totalUtf16Length += charDataList[i].CharObject.CodePoint.CharLength;
        }

        // 最后一个槽位是末尾哨兵，不对应任何真实字符。
        // 作用不只是“便于二分查找”，更重要的是让最后一个字符也能像其他字符一样，
        // 通过 [start, nextStart) 方式计算覆盖范围，而不需要为末尾单独写分支。
        charDataUtf16StartIndexSpan[charDataList.Count] = totalUtf16Length;

        // 这里收集的是“逻辑上的 cluster 起点”，而不是 glyph 顺序。
        // RTL 场景下 glyph 输出顺序可能是倒序，因此需要先排序，恢复成逻辑顺序的边界表。
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

        // 排序之后，logicalClusterSpan[i] 才能表示“第 i 个逻辑 cluster 的起点”。
        logicalClusterSpan = logicalClusterSpan[..logicalClusterCount];
        logicalClusterSpan.Sort();

        // 去重是为了解决“多个 glyph 属于同一个 cluster”的情况。
        // 例如一个逻辑字符被拆成多个 glyph 时，会出现重复的 GlyphCluster。
        // 对当前算法来说，我们只需要逻辑边界，不需要重复的边界项；
        // 如果不去重，后续拿“下一个 cluster 起点”计算范围时会得到零宽度区间，增加理解成本。
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

            // 一个 glyph 需要两个边界：
            // 1. 当前 cluster 起点，对应当前 glyph 应该回填到哪个 CharData；
            // 2. 下一个逻辑 cluster 起点，对应当前 glyph 覆盖范围的右边界（开区间）。
            // 这也是这里需要多次调用 FindXxxIndex 的原因：它们分别回答不同的问题。
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

        // 在已经去重并排序后的 logicalClusterSpan 里查找当前 cluster 的逻辑序号。
        // right 初始化为 Length - 1，是因为二分查找的右边界始终是“最后一个有效元素的索引”。
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

        // 根据 UTF-16 索引，找到“起点小于等于该索引”的最后一个真实 CharData。
        // 这里的 charDataUtf16StartIndexSpan 包含一个末尾哨兵，因此：
        // - 二分的 right 可以安全地取 Length - 1，表示允许命中哨兵
        // - 最终返回值要 clamp 到 Length - 2，避免把哨兵错当成真实字符索引
        //   也就是说，这里的 -2 不是魔法数字，而是“排除最后一个哨兵槽位”后的最后一个真实字符索引。
        // 取上中位数 ((right - left + 1) >> 1) 是为了查找“最后一个 <= 目标值”的位置，避免死循环。
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
