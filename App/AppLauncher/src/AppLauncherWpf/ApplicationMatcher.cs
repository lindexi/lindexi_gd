namespace AppLauncherWpf;

internal static class ApplicationMatcher
{
    public static IEnumerable<ApplicationEntry> Find(
        IEnumerable<ApplicationEntry> applications,
        string searchText)
    {
        ArgumentNullException.ThrowIfNull(applications);

        string query = searchText.Trim();
        if (query.Length == 0)
        {
            return applications.OrderBy(application => application.Name, StringComparer.CurrentCultureIgnoreCase);
        }

        return applications
            .Select(application => new
            {
                Application = application,
                Score = application.SearchNames.Max(name => GetScore(name, query))
            })
            .Where(match => match.Score > 0)
            .OrderByDescending(match => match.Score)
            .ThenBy(match => match.Application.Name, StringComparer.CurrentCultureIgnoreCase)
            .Select(match => match.Application);
    }

    private static int GetScore(string candidate, string query)
    {
        if (candidate.Equals(query, StringComparison.CurrentCultureIgnoreCase))
        {
            return 4000;
        }

        if (candidate.StartsWith(query, StringComparison.CurrentCultureIgnoreCase))
        {
            return 3000 - Math.Min(candidate.Length - query.Length, 500);
        }

        int containsIndex = candidate.IndexOf(query, StringComparison.CurrentCultureIgnoreCase);
        if (containsIndex >= 0)
        {
            return 2000 - Math.Min(containsIndex * 10 + candidate.Length - query.Length, 500);
        }

        int subsequenceScore = GetSubsequenceScore(candidate, query);
        return subsequenceScore >= query.Length * 8 ? 1000 + subsequenceScore : 0;
    }

    private static int GetSubsequenceScore(string candidate, string query)
    {
        int queryIndex = 0;
        int score = 0;
        int previousMatchIndex = -2;

        for (int candidateIndex = 0; candidateIndex < candidate.Length && queryIndex < query.Length; candidateIndex++)
        {
            if (char.ToUpperInvariant(candidate[candidateIndex]) != char.ToUpperInvariant(query[queryIndex]))
            {
                continue;
            }

            score += candidateIndex == previousMatchIndex + 1 ? 14 : 8;
            if (candidateIndex == 0 || char.IsWhiteSpace(candidate[candidateIndex - 1]))
            {
                score += 6;
            }

            previousMatchIndex = candidateIndex;
            queryIndex++;
        }

        return queryIndex == query.Length ? score : 0;
    }
}
