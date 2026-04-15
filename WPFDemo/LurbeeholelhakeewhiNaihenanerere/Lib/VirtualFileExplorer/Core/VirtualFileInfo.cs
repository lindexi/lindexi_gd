using System.IO;
using System.Runtime.Serialization;

namespace VirtualFileExplorer.Core;

/// <summary>
/// 表示一个虚拟的文件信息
/// </summary>
public class VirtualFileInfo : VirtualFileSystemEntry
{
    public VirtualFileInfo(string id, string name, VirtualFolderInfo ownerFolder)
        : base(id, name, ownerFolder)
    {
        _extension = Path.GetExtension(name);
        IconGlyph = FileIconGlyphHelper.GetFileGlyph(_extension);
    }

    private string _extension;
    private long? _length;

    public override VirtualFileSystemEntryType EntryType => VirtualFileSystemEntryType.File;

    [DataMember(Name = "extension")]
    public string Extension
    {
        get => _extension;
        internal set
        {
            if (SetField(ref _extension, value))
            {
                IconGlyph = FileIconGlyphHelper.GetFileGlyph(value);
                OnPropertyChanged(nameof(DisplayType));
            }
        }
    }

    [DataMember(Name = "length")]
    public long? Length
    {
        get => _length;
        internal set
        {
            if (SetField(ref _length, value))
            {
                OnPropertyChanged(nameof(DisplaySize));
            }
        }
    }

    public override string DisplayType => string.IsNullOrWhiteSpace(Extension)
        ? "文件"
        : $"{Extension.TrimStart('.').ToUpperInvariant()} 文件";

    public override string DisplaySize => Length is null ? string.Empty : FormatFileSize(Length.Value);

    private static string FormatFileSize(long length)
    {
        var sizeUnits = new[] { "B", "KB", "MB", "GB", "TB" };
        var value = (double) length;
        var index = 0;
        while (value >= 1024 && index < sizeUnits.Length - 1)
        {
            value /= 1024;
            index++;
        }

        return index == 0 ? $"{value:0} {sizeUnits[index]}" : $"{value:0.##} {sizeUnits[index]}";
    }
}