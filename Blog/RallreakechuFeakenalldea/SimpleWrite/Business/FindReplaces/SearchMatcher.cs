using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SimpleWrite.Business.FindReplaces;

internal sealed class SearchMatcher
{
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
        ArgumentNullException.ThrowIfNull(searchText);
        if (searchText.Length == 0)
        {
            throw new ArgumentException("Value cannot be empty.", nameof(searchText));
        }

        if (!useRegularExpression)
        {
            searchMatcher = new SearchMatcher(searchText, useRegularExpression, isCaseInsensitive, regex: null);
            return true;
        }

        try
        {
            var regexOptions = RegexOptions.CultureInvariant;
            if (isCaseInsensitive)
            {
                regexOptions |= RegexOptions.IgnoreCase;
            }

            searchMatcher = new SearchMatcher(searchText, useRegularExpression, isCaseInsensitive, new Regex(searchText, regexOptions));
            return true;
        }
        catch (ArgumentException)
        {
            searchMatcher = null;
            return false;
        }
    }

    public IReadOnlyList<SearchMatchResult> FindMatches(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (text.Length == 0)
        {
            return [];
        }

        return _regex is null
            ? FindLiteralMatches(text)
            : FindRegexMatches(text);
    }

    public string GetReplacementText(string sourceText, SearchMatchResult match, string replaceText)
    {
        ArgumentNullException.ThrowIfNull(sourceText);
        ArgumentNullException.ThrowIfNull(match.Value);
        ArgumentNullException.ThrowIfNull(replaceText);

        if (_regex is null)
        {
            return replaceText;
        }

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
                return regexMatch.Result(replaceText);
            }

            if (regexMatch.Index > match.StartOffset)
            {
                break;
            }
        }

        return match.Value;
    }

    private IReadOnlyList<SearchMatchResult> FindLiteralMatches(string text)
    {
        var matchList = new List<SearchMatchResult>();
        var startIndex = 0;
        var stringComparison = IsCaseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        while (startIndex <= text.Length - SearchText.Length)
        {
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

internal readonly record struct SearchMatchResult(int StartOffset, int Length, string Value);
