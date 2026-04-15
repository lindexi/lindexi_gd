using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TxtChunking
{
    public sealed class ChunkingOptions
    {
        /// <summary>类似 MAX_TOKENS_PER_CHUNK（默认 450）</summary>
        public int MaxTokensPerChunk { get; init; } = 450;

        /// <summary>soft_limit 比例（项目注释里约 0.8）</summary>
        public double SoftLimitRatio { get; init; } = 0.80;

        /// <summary>段落分隔：一个或多个空行</summary>
        public Regex ParagraphSplitRegex { get; init; } =
            new Regex(@"\r?\n\s*\r?\n+", RegexOptions.Compiled);

        /// <summary>句子终止符集合（可配，类似 SENTENCE_TERMINATORS）</summary>
        public char[] SentenceTerminators { get; init; } =
            new[] { '.', '!', '?', '。', '！', '？', ';', '；' };

        /// <summary>句子切分时，允许的最小句子长度（防止碎片太多）</summary>
        public int MinSentenceLengthChars { get; init; } = 1;

        /// <summary>是否对段落/句子做 trim</summary>
        public bool TrimUnits { get; init; } = true;
    }

    public sealed record ChunkResult(
        string ContextBefore,
        string MainContent,
        string ContextAfter
    )
    {
        public override string ToString()
        {
            // 你如果只想“输出各个分块的内容”，可以只用 MainContent。
            return MainContent;
        }
    }

    public interface ITokenEstimator
    {
        int EstimateTokens(string text);
    }

    /// <summary>
    /// 默认的近似 token 估算器：
    /// - 英文/数字按“词”算
    /// - CJK 字符按“字符”算（近似）
    /// - 标点/空白做轻微计数
    /// 目的：实现分块策略与可替换接口，而不是追求 tokenizer 级精确。
    /// </summary>
    public sealed class HeuristicTokenEstimator : ITokenEstimator
    {
        private static readonly Regex WordRegex = new Regex(@"[A-Za-z0-9]+", RegexOptions.Compiled);
        private static readonly Regex CjkRegex = new Regex(@"[\p{IsCJKUnifiedIdeographs}\p{IsHiragana}\p{IsKatakana}\p{IsHangulSyllables}]", RegexOptions.Compiled);

        public int EstimateTokens(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;

            int words = WordRegex.Matches(text).Count;
            int cjk = CjkRegex.Matches(text).Count;

            // 其余字符（标点等）给少量权重，避免严重低估
            int others = Math.Max(0, text.Length - (words > 0 ? WordRegex.Matches(text).Sum(m => m.Length) : 0) - cjk);
            int punctLike = others / 8;

            // 经验系数：让估算略偏保守
            double estimate = words * 1.0 + cjk * 1.0 + punctLike * 1.0;
            return (int) Math.Ceiling(estimate);
        }
    }

    public sealed class TxtTokenChunker
    {
        private readonly ChunkingOptions _opt;
        private readonly ITokenEstimator _tokenEstimator;

        public TxtTokenChunker(ChunkingOptions options, ITokenEstimator? tokenEstimator = null)
        {
            _opt = options ?? throw new ArgumentNullException(nameof(options));
            _tokenEstimator = tokenEstimator ?? new HeuristicTokenEstimator();

            if (_opt.MaxTokensPerChunk <= 0) throw new ArgumentOutOfRangeException(nameof(options.MaxTokensPerChunk));
            if (_opt.SoftLimitRatio <= 0 || _opt.SoftLimitRatio > 1.0) throw new ArgumentOutOfRangeException(nameof(options.SoftLimitRatio));
        }

        public List<ChunkResult> ChunkFile(string txtPath, Encoding? encoding = null)
        {
            if (string.IsNullOrWhiteSpace(txtPath)) throw new ArgumentNullException(nameof(txtPath));
            var text = File.ReadAllText(txtPath, encoding ?? Encoding.UTF8);
            return ChunkText(text);
        }

        public List<ChunkResult> ChunkText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return new List<ChunkResult>();

            var paragraphs = SplitIntoParagraphs(text);
            if (paragraphs.Count == 0) return new List<ChunkResult>();

            // 1) 先做“raw chunk”：只生成 main_content（段落优先，段落超限则句子分裂）
            var rawMainChunks = BuildRawChunks(paragraphs);

            // 2) 再包装成 structured chunks：context_before / context_after
            var results = new List<ChunkResult>(rawMainChunks.Count);
            for (int i = 0; i < rawMainChunks.Count; i++)
            {
                string contextBefore = "";
                if (i > 0)
                {
                    var prevParas = SplitIntoParagraphs(rawMainChunks[i - 1]);
                    contextBefore = prevParas.Count > 0 ? prevParas[^1] : "";
                }

                string contextAfter = "";
                if (i < rawMainChunks.Count - 1)
                {
                    var nextParas = SplitIntoParagraphs(rawMainChunks[i + 1]);
                    contextAfter = nextParas.Count > 0 ? nextParas[0] : "";
                }

                results.Add(new ChunkResult(contextBefore, rawMainChunks[i], contextAfter));
            }

            return results;
        }

        /// <summary>按空行分段</summary>
        private List<string> SplitIntoParagraphs(string text)
        {
            var parts = _opt.ParagraphSplitRegex
                .Split(text)
                .Select(p => _opt.TrimUnits ? p.Trim() : p)
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToList();

            return parts;
        }

        private List<string> BuildRawChunks(List<string> paragraphs)
        {
            int maxTokens = _opt.MaxTokensPerChunk;
            int softLimit = (int) Math.Floor(maxTokens * _opt.SoftLimitRatio);

            var chunks = new List<string>();
            var current = new List<string>();
            int currentTokens = 0;

            void FinalizeCurrent()
            {
                if (current.Count == 0) return;
                chunks.Add(string.Join("\n\n", current).Trim());
                current.Clear();
                currentTokens = 0;
            }

            foreach (var para in paragraphs)
            {
                int paraTokens = _tokenEstimator.EstimateTokens(para);

                // 单段落就超限：按句子拆成多个“段落单元”再走同样逻辑
                if (paraTokens > maxTokens)
                {
                    // 先把已有 chunk 收口，保证逻辑清晰
                    FinalizeCurrent();

                    var sentenceUnits = SplitParagraphIntoSentenceUnits(para);
                    foreach (var unit in sentenceUnits)
                    {
                        // unit 仍然可能很长（极端长句），这里再做硬截断 fallback（可选）
                        var unitTokens = _tokenEstimator.EstimateTokens(unit);
                        if (unitTokens > maxTokens)
                        {
                            // 退化：按字符切（尽量不发生；真实项目可用更强策略）
                            foreach (var piece in HardSplitByChars(unit, 2000))
                            {
                                chunks.Add(piece.Trim());
                            }
                            continue;
                        }

                        // 句子单元累积成 chunk（使用 soft_limit + max_tokens）
                        if (current.Count == 0)
                        {
                            current.Add(unit);
                            currentTokens = unitTokens;
                            continue;
                        }

                        // 用 "\n" 连接句子单元（在“超长段落拆句子”的场景）
                        int joinTokens = _tokenEstimator.EstimateTokens("\n");
                        int candidateTokens = currentTokens + joinTokens + unitTokens;

                        if (candidateTokens > maxTokens)
                        {
                            FinalizeCurrent();
                            current.Add(unit);
                            currentTokens = unitTokens;
                        }
                        else
                        {
                            current.Add(unit);
                            currentTokens = candidateTokens;

                            // 达到 soft limit，倾向于尽快收口（与项目“累积到 soft_limit”一致）
                            if (currentTokens >= softLimit)
                                FinalizeCurrent();
                        }
                    }

                    FinalizeCurrent();
                    continue;
                }

                // 正常段落累积：段落之间用 "\n\n" 分隔
                if (current.Count == 0)
                {
                    current.Add(para);
                    currentTokens = paraTokens;
                    continue;
                }

                int sepTokens = _tokenEstimator.EstimateTokens("\n\n");
                int nextTokens = currentTokens + sepTokens + paraTokens;

                if (nextTokens > maxTokens)
                {
                    FinalizeCurrent();
                    current.Add(para);
                    currentTokens = paraTokens;
                }
                else
                {
                    current.Add(para);
                    currentTokens = nextTokens;

                    // 达到 soft limit 就 finalize（减少风险，避免越积越满）
                    if (currentTokens >= softLimit)
                        FinalizeCurrent();
                }
            }

            // 收尾
            if (current.Count > 0)
                chunks.Add(string.Join("\n\n", current).Trim());

            return chunks;
        }

        /// <summary>
        /// 将超长段落拆成句子单元（保留终止符）。
        /// 这里相当于项目里“单段落超限 -> split into sentences”。
        /// </summary>
        private List<string> SplitParagraphIntoSentenceUnits(string paragraph)
        {
            var units = new List<string>();
            if (string.IsNullOrWhiteSpace(paragraph)) return units;

            var sb = new StringBuilder();
            for (int i = 0; i < paragraph.Length; i++)
            {
                char c = paragraph[i];
                sb.Append(c);

                if (_opt.SentenceTerminators.Contains(c))
                {
                    var s = sb.ToString();
                    if (_opt.TrimUnits) s = s.Trim();
                    if (s.Length >= _opt.MinSentenceLengthChars && !string.IsNullOrWhiteSpace(s))
                        units.Add(s);
                    sb.Clear();
                }
            }

            // 末尾残余
            var tail = sb.ToString();
            if (_opt.TrimUnits) tail = tail.Trim();
            if (!string.IsNullOrWhiteSpace(tail))
                units.Add(tail);

            // 若完全没有终止符，退化为整段一个 unit
            if (units.Count == 0 && !string.IsNullOrWhiteSpace(paragraph))
                units.Add(_opt.TrimUnits ? paragraph.Trim() : paragraph);

            return units;
        }

        /// <summary>
        /// 极端兜底：按字符硬切。你也可以删掉这段，强制要求句子拆分足够。
        /// </summary>
        private static IEnumerable<string> HardSplitByChars(string s, int maxChars)
        {
            if (string.IsNullOrEmpty(s)) yield break;
            if (maxChars <= 0) throw new ArgumentOutOfRangeException(nameof(maxChars));

            for (int i = 0; i < s.Length; i += maxChars)
            {
                int len = Math.Min(maxChars, s.Length - i);
                yield return s.Substring(i, len);
            }
        }
    }

    internal static class RegexExtensions
    {
        public static int Sum(this MatchCollection matches, Func<Match, int> selector)
        {
            int total = 0;
            foreach (Match m in matches) total += selector(m);
            return total;
        }
    }
}