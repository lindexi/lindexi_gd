using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using AvaloniaAgentLib.Tools;
using AvaloniaAgentLib.ViewModel;

using LightTextEditorPlus;
using LightTextEditorPlus.Core.Carets;

using Microsoft.Extensions.AI;

using SimpleWrite.Business.TextEditors.CommandPatterns;

namespace SimpleWrite.Business.CopilotCommandPatterns;

sealed class PolishSelectedTextCommandPattern(CopilotViewModel copilotViewModel) : ICommandPattern
{
    public int Priority => 300;

    public bool SupportSingleLine => false;

    public string Title => "润色选中文本";

    public ValueTask<bool> IsMatchAsync(string text)
    {
        return ValueTask.FromResult(!string.IsNullOrWhiteSpace(text));
    }

    public async Task DoAsync(string text, TextEditor textEditor)
    {
        ArgumentNullException.ThrowIfNull(textEditor);

        Selection selection = textEditor.CurrentSelection;
        if (selection.IsEmpty)
        {
            return;
        }

        string selectedText = textEditor.GetText(in selection);
        if (string.IsNullOrWhiteSpace(selectedText))
        {
            return;
        }

        var trackedSelectionSnapshot = TrackedSelectionSnapshot.Create(textEditor, selection, selectedText);
        var generatedTextSubmissionTool = new GeneratedTextSubmissionTool();
        List<AITool> toolList =
        [
            generatedTextSubmissionTool.CreateTool("submit_polished_text", "提交润色后的完整文本内容。"),
        ];

        string prompt =
            $"""
             你是文本润色助手。请在不改变原意、不增加额外信息的前提下润色给定文本，保留原有语言、段落结构和 Markdown/代码格式。
             完成后你必须调用工具 `submit_polished_text`，参数传入润色后的完整文本，不要在聊天中直接输出最终结果。
             待润色文本：
             {selectedText}
             """;

        await copilotViewModel.SendMessageAsync(prompt, withHistory: false, toolList, ChatToolMode.RequireAny);

        if (!generatedTextSubmissionTool.HasSubmittedText || string.IsNullOrWhiteSpace(generatedTextSubmissionTool.SubmittedText))
        {
            await copilotViewModel.AddLocalConversationAsync("润色选中文本", "未替换：大模型没有通过工具返回润色结果。");
            return;
        }

        if (!trackedSelectionSnapshot.TryResolveCurrentSelection(textEditor, out Selection currentSelection, out string reason))
        {
            await copilotViewModel.AddLocalConversationAsync("润色选中文本", reason);
            return;
        }

        string currentText = textEditor.GetText(in currentSelection);
        if (!string.Equals(currentText, trackedSelectionSnapshot.OriginalText, StringComparison.Ordinal))
        {
            await copilotViewModel.AddLocalConversationAsync("润色选中文本", "未替换：原始选中文本在等待润色期间已经发生变化。");
            return;
        }

        textEditor.EditAndReplace(generatedTextSubmissionTool.SubmittedText, currentSelection);
    }
}

file sealed class TrackedSelectionSnapshot
{
    private const int ContextLength = 32;

    private TrackedSelectionSnapshot(Selection originalSelection, string originalText, string prefixContext, string suffixContext)
    {
        OriginalSelection = originalSelection;
        OriginalText = originalText;
        PrefixContext = prefixContext;
        SuffixContext = suffixContext;
    }

    public Selection OriginalSelection { get; }

    public string OriginalText { get; }

    private string PrefixContext { get; }

    private string SuffixContext { get; }

    public static TrackedSelectionSnapshot Create(TextEditor textEditor, Selection selection, string originalText)
    {
        string fullText = textEditor.Text;
        int frontOffset = selection.FrontOffset.Offset;
        int behindOffset = selection.BehindOffset.Offset;

        int prefixLength = Math.Min(ContextLength, frontOffset);
        int suffixLength = Math.Min(ContextLength, fullText.Length - behindOffset);

        string prefixContext = fullText.Substring(frontOffset - prefixLength, prefixLength);
        string suffixContext = fullText.Substring(behindOffset, suffixLength);
        return new TrackedSelectionSnapshot(selection, originalText, prefixContext, suffixContext);
    }

    public bool TryResolveCurrentSelection(TextEditor textEditor, out Selection selection, out string reason)
    {
        string currentText = textEditor.Text;
        int originalFrontOffset = OriginalSelection.FrontOffset.Offset;
        if (CanMatchAt(currentText, originalFrontOffset))
        {
            selection = CreateSelection(originalFrontOffset, OriginalText.Length);
            reason = string.Empty;
            return true;
        }

        List<SelectionCandidate> candidateList = FindCandidates(currentText);
        if (candidateList.Count == 0)
        {
            selection = default;
            reason = "未替换：原始选中文本在等待润色期间已经发生变化。";
            return false;
        }

        SelectionCandidate bestCandidate = candidateList[0];
        bool isAmbiguous = false;
        for (int i = 1; i < candidateList.Count; i++)
        {
            SelectionCandidate candidate = candidateList[i];

            if (candidate.Score > bestCandidate.Score)
            {
                bestCandidate = candidate;
                isAmbiguous = false;
                continue;
            }

            if (candidate.Score == bestCandidate.Score)
            {
                if (candidate.Distance < bestCandidate.Distance)
                {
                    bestCandidate = candidate;
                    isAmbiguous = false;
                }
                else if (candidate.Distance == bestCandidate.Distance)
                {
                    isAmbiguous = true;
                }
            }
        }

        if (bestCandidate.Score == 0)
        {
            selection = default;
            reason = "未替换：原始选中文本所在位置附近内容已经变化，无法安全替换。";
            return false;
        }

        if (isAmbiguous)
        {
            selection = default;
            reason = "未替换：文档中存在多处相同文本，无法确认应该替换哪一处。";
            return false;
        }

        selection = CreateSelection(bestCandidate.StartIndex, OriginalText.Length);
        reason = string.Empty;
        return true;
    }

    private List<SelectionCandidate> FindCandidates(string currentText)
    {
        var candidateList = new List<SelectionCandidate>();
        int searchStartIndex = 0;
        while (searchStartIndex < currentText.Length)
        {
            int foundIndex = currentText.IndexOf(OriginalText, searchStartIndex, StringComparison.Ordinal);
            if (foundIndex < 0)
            {
                break;
            }

            int score = CalculateContextScore(currentText, foundIndex);
            int distance = Math.Abs(foundIndex - OriginalSelection.FrontOffset.Offset);
            candidateList.Add(new SelectionCandidate(foundIndex, score, distance));
            searchStartIndex = foundIndex + Math.Max(1, OriginalText.Length);
        }

        return candidateList;
    }

    private int CalculateContextScore(string currentText, int startIndex)
    {
        int score = 0;
        if (MatchesPrefixContext(currentText, startIndex))
        {
            score++;
        }

        if (MatchesSuffixContext(currentText, startIndex))
        {
            score++;
        }

        return score;
    }

    private bool MatchesPrefixContext(string currentText, int startIndex)
    {
        if (PrefixContext.Length == 0)
        {
            return true;
        }

        if (startIndex < PrefixContext.Length)
        {
            return false;
        }

        return string.Compare(currentText, startIndex - PrefixContext.Length, PrefixContext, 0, PrefixContext.Length, StringComparison.Ordinal) == 0;
    }

    private bool MatchesSuffixContext(string currentText, int startIndex)
    {
        if (SuffixContext.Length == 0)
        {
            return true;
        }

        int suffixStartIndex = startIndex + OriginalText.Length;
        if (suffixStartIndex + SuffixContext.Length > currentText.Length)
        {
            return false;
        }

        return string.Compare(currentText, suffixStartIndex, SuffixContext, 0, SuffixContext.Length, StringComparison.Ordinal) == 0;
    }

    private bool CanMatchAt(string currentText, int startIndex)
    {
        if (startIndex < 0 || startIndex + OriginalText.Length > currentText.Length)
        {
            return false;
        }

        return string.Compare(currentText, startIndex, OriginalText, 0, OriginalText.Length, StringComparison.Ordinal) == 0;
    }

    private static Selection CreateSelection(int startOffset, int length)
    {
        var frontOffset = new CaretOffset(startOffset);
        var behindOffset = new CaretOffset(startOffset + length);
        return new Selection(frontOffset, behindOffset);
    }

    private readonly record struct SelectionCandidate(int StartIndex, int Score, int Distance);
}
