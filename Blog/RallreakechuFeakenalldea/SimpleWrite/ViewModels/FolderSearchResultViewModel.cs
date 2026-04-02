using System.IO;

using SimpleWrite.Business.FolderExplorers;

namespace SimpleWrite.ViewModels;

public class FolderSearchResultViewModel
{
    internal FolderSearchResultViewModel(FolderSearchResult folderSearchResult)
    {
        FileInfo = new FileInfo(folderSearchResult.FilePath);
        RelativePath = folderSearchResult.RelativePath;
        PreviewText = folderSearchResult.PreviewText;
        MatchCount = folderSearchResult.MatchCount;
        FirstMatchOffset = folderSearchResult.FirstMatchOffset;
        MatchLength = folderSearchResult.MatchLength;
    }

    internal FileInfo FileInfo { get; }

    public string RelativePath { get; }

    public string PreviewText { get; }

    public int MatchCount { get; }

    internal int FirstMatchOffset { get; }

    internal int MatchLength { get; }
}
