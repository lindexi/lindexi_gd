using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SimpleWrite.Business.FileHandlers;

namespace SimpleWrite.Business.FolderExplorers;

internal class FolderSearchService
{
    private const int PreviewRadius = 24;
    private const long MaxSearchFileSize = 2 * 1024 * 1024;

    public async Task<IReadOnlyList<FolderSearchResult>> SearchAsync(DirectoryInfo rootDirectory, string searchText)
    {
        ArgumentNullException.ThrowIfNull(rootDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(searchText);

        var resultList = new List<FolderSearchResult>();
        var textFileReader = new TextFileReader();

        foreach (var file in EnumerateFiles(rootDirectory).OrderBy(temp => temp.FullName, StringComparer.OrdinalIgnoreCase))
        {
            if (file.Length > MaxSearchFileSize)
            {
                continue;
            }

            string text;
            try
            {
                text = await textFileReader.ReadAllTextAsync(file);
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }
            catch (DirectoryNotFoundException)
            {
                continue;
            }
            catch (FileNotFoundException)
            {
                continue;
            }
            catch (IOException)
            {
                continue;
            }
            catch (DecoderFallbackException)
            {
                continue;
            }
            catch (ArgumentException)
            {
                continue;
            }

            var firstMatchIndex = text.IndexOf(searchText, StringComparison.Ordinal);
            if (firstMatchIndex < 0)
            {
                continue;
            }

            var matchCount = CountMatches(text, searchText);
            var relativePath = Path.GetRelativePath(rootDirectory.FullName, file.FullName);
            var previewText = CreatePreview(text, firstMatchIndex, searchText.Length);

            resultList.Add(new FolderSearchResult(file.FullName, relativePath, previewText, matchCount, firstMatchIndex, searchText.Length));
        }

        return resultList;
    }

    private static IEnumerable<FileInfo> EnumerateFiles(DirectoryInfo directoryInfo)
    {
        IEnumerable<FileInfo> fileList;
        try
        {
            fileList = directoryInfo.EnumerateFiles();
        }
        catch (UnauthorizedAccessException)
        {
            yield break;
        }
        catch (DirectoryNotFoundException)
        {
            yield break;
        }
        catch (IOException)
        {
            yield break;
        }

        foreach (var file in fileList)
        {
            yield return file;
        }

        IEnumerable<DirectoryInfo> directoryList;
        try
        {
            directoryList = directoryInfo.EnumerateDirectories();
        }
        catch (UnauthorizedAccessException)
        {
            yield break;
        }
        catch (DirectoryNotFoundException)
        {
            yield break;
        }
        catch (IOException)
        {
            yield break;
        }

        foreach (var childDirectory in directoryList)
        {
            foreach (var childFile in EnumerateFiles(childDirectory))
            {
                yield return childFile;
            }
        }
    }

    private static int CountMatches(string text, string searchText)
    {
        var count = 0;
        var startIndex = 0;

        while (startIndex <= text.Length - searchText.Length)
        {
            var matchIndex = text.IndexOf(searchText, startIndex, StringComparison.Ordinal);
            if (matchIndex < 0)
            {
                break;
            }

            count++;
            startIndex = matchIndex + Math.Max(searchText.Length, 1);
        }

        return count;
    }

    private static string CreatePreview(string text, int matchIndex, int matchLength)
    {
        var previewStart = Math.Max(0, matchIndex - PreviewRadius);
        var previewLength = Math.Min(text.Length - previewStart, matchLength + PreviewRadius * 2);
        var preview = text.Substring(previewStart, previewLength)
            .Replace("\r", " ")
            .Replace("\n", " ")
            .Trim();

        if (previewStart > 0)
        {
            preview = $"...{preview}";
        }

        if (previewStart + previewLength < text.Length)
        {
            preview = $"{preview}...";
        }

        return preview;
    }
}

internal readonly record struct FolderSearchResult(
    string FilePath,
    string RelativePath,
    string PreviewText,
    int MatchCount,
    int FirstMatchOffset,
    int MatchLength);
