using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace SimpleWrite.Business.FindReplaces;

internal sealed class SearchMatcher
{
    private static readonly TimeSpan DefaultRegexMatchTimeout = TimeSpan.FromSeconds(3);

    private SearchMatcher(string searchText, bool useRegularExpression, bool isCaseInsensitive, Regex? regex)
    {
        SearchText = searchText;
        UseRegularExpression = useRegularExpression;
        IsCaseInsensitive = isCaseInsensitive;
        _regex = regex;
    }

    public string SearchText { get; }

    public bool UseRegularExpression { get; }

    public bool IsCaseInsensitive { get; }

    public static bool TryCreate(string searchText, bool useRegularExpression, bool isCaseInsensitive, out SearchMatcher? searchMatcher)
    {
        if (string.IsNullOrEmpty(searchText))
        {
            searchMatcher = null;
            return false;
        }

        if (!useRegularExpression)
        {
            searchMatcher = new SearchMatcher(searchText, useRegularExpression, isCaseInsensitive, regex: null);
            return true;
        }

        if (!LooksLikeCompleteRegularExpression(searchText))
        {
            searchMatcher = null;
            return false;
        }

        try
        {
            var regexOptions = RegexOptions.CultureInvariant;
            if (isCaseInsensitive)
            {
                regexOptions |= RegexOptions.IgnoreCase;
            }

            searchMatcher = new SearchMatcher(searchText, useRegularExpression, isCaseInsensitive, new Regex(searchText, regexOptions, DefaultRegexMatchTimeout));
            return true;
        }
        catch (ArgumentException)
        {
            searchMatcher = null;
            return false;
        }
    }

    public Task<SearchExecutionResult> FindMatchesAsync(string text, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(text);
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(() => FindMatchesCore(text, cancellationToken), cancellationToken);
    }

    public bool TryGetReplacementText(string sourceText, SearchMatchResult match, string replaceText, out string replacementText)
    {
        ArgumentNullException.ThrowIfNull(sourceText);
        ArgumentNullException.ThrowIfNull(match.Value);
        ArgumentNullException.ThrowIfNull(replaceText);

        if (_regex is null)
        {
            replacementText = replaceText;
            return true;
        }

        try
        {
            for (var regexMatch = _regex.Match(sourceText, match.StartOffset);
                 regexMatch.Success;
                 regexMatch = regexMatch.NextMatch())
            {
                if (regexMatch.Length == 0)
                {
                    continue;
                }

                if (regexMatch.Index == match.StartOffset && regexMatch.Length == match.Length)
                {
                    replacementText = regexMatch.Result(replaceText);
                    return true;
                }

                if (regexMatch.Index > match.StartOffset)
                {
                    break;
                }
            }
        }
        catch (RegexMatchTimeoutException)
        {
            replacementText = string.Empty;
            return false;
        }

        replacementText = match.Value;
        return true;
    }

    private SearchExecutionResult FindMatchesCore(string text, CancellationToken cancellationToken)
    {
        if (text.Length == 0)
        {
            return new SearchExecutionResult([], false);
        }

        try
        {
            var matchList = _regex is null
                ? FindLiteralMatches(text, cancellationToken)
                : FindRegexMatches(text);

            return new SearchExecutionResult(matchList, false);
        }
        catch (RegexMatchTimeoutException)
        {
            return new SearchExecutionResult([], true);
        }
    }

    private IReadOnlyList<SearchMatchResult> FindLiteralMatches(string text, CancellationToken cancellationToken)
    {
        var matchList = new List<SearchMatchResult>();
        var startIndex = 0;
        var stringComparison = IsCaseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        while (startIndex <= text.Length - SearchText.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var matchIndex = text.IndexOf(SearchText, startIndex, stringComparison);
            if (matchIndex < 0)
            {
                break;
            }

            matchList.Add(new SearchMatchResult(matchIndex, SearchText.Length, text.Substring(matchIndex, SearchText.Length)));
            startIndex = matchIndex + SearchText.Length;
        }

        return matchList;
    }

    private static bool LooksLikeCompleteRegularExpression(string searchText)
    {
        var isEscaped = false;
        var groupDepth = 0;
        var bracketDepth = 0;
        var braceDepth = 0;

        foreach (var character in searchText)
        {
            if (isEscaped)
            {
                isEscaped = false;
                continue;
            }

            if (character == '\\')
            {
                isEscaped = true;
                continue;
            }

            if (character == '[')
            {
                bracketDepth++;
                continue;
            }

            if (character == ']' && bracketDepth > 0)
            {
                bracketDepth--;
                continue;
            }

            if (bracketDepth > 0)
            {
                continue;
            }

            switch (character)
            {
                case '(':
                    groupDepth++;
                    break;
                case ')':
                    if (groupDepth > 0)
                    {
                        groupDepth--;
                    }
                    break;
                case '{':
                    braceDepth++;
                    break;
                case '}':
                    if (braceDepth > 0)
                    {
                        braceDepth--;
                    }
                    break;
            }
        }

        return !isEscaped && groupDepth == 0 && bracketDepth == 0 && braceDepth == 0;
    }

    private IReadOnlyList<SearchMatchResult> FindRegexMatches(string text)
    {
        var matchList = new List<SearchMatchResult>();

        foreach (Match match in _regex!.Matches(text))
        {
            if (!match.Success || match.Length == 0)
            {
                continue;
            }

            matchList.Add(new SearchMatchResult(match.Index, match.Length, match.Value));
        }

        return matchList;
    }

    private readonly Regex? _regex;
}

internal readonly record struct SearchExecutionResult(IReadOnlyList<SearchMatchResult> MatchList, bool IsTimedOut);

internal readonly record struct SearchMatchResult(int StartOffset, int Length, string Value);
