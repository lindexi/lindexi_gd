using System.Text;

namespace XiaoXiIme.Dictionary;

internal static class DictionaryPackageFormat
{
    internal const int ShardVersion = 1;
    internal const string ManifestFileName = "manifest.json";
    internal const string CandidatesFileName = "candidates.bin";
    internal const string ExactIndexFileName = "exact-index.bin";
    internal const string PrefixIndexFileName = "prefix-index.bin";
    internal const string ShapeIndexFileName = "shape-index.bin";
    internal const string SymbolsFileName = "symbols.bin";
    internal const string CandidatesRole = "candidates";
    internal const string ExactIndexRole = "exactIndex";
    internal const string PrefixIndexRole = "prefixIndex";
    internal const string ShapeIndexRole = "shapeIndex";
    internal const string SymbolsRole = "symbols";
    internal const string FullPinyinInputScheme = "fullPinyin";
    internal const string XiaoheDoublePinyinInputScheme = "xiaoheDoublePinyin";
    internal const int MaxManifestBytes = 1024 * 1024;
    internal const long MaxShardBytes = 256L * 1024 * 1024;
    internal const long MaxTotalShardBytes = 512L * 1024 * 1024;
    internal const int MaxCandidates = 2_000_000;
    internal const int MaxIndexKeys = 2_000_000;
    internal const int MaxCandidatesPerKey = 1024;
    internal const int MaxManifestItems = 128;
    internal const int MaxStringUtf8Bytes = 4096;

    internal static ReadOnlySpan<byte> CandidatesMagic => "XXICANDS"u8;
    internal static ReadOnlySpan<byte> ExactIndexMagic => "XXIEXACT"u8;
    internal static ReadOnlySpan<byte> PrefixIndexMagic => "XXIPREFX"u8;
    internal static ReadOnlySpan<byte> ShapeIndexMagic => "XXISHAPE"u8;
    internal static ReadOnlySpan<byte> SymbolsMagic => "XXISYMBL"u8;

    internal static void WriteHeader(BinaryWriter writer, ReadOnlySpan<byte> magic)
    {
        writer.Write(magic);
        writer.Write(ShardVersion);
    }

    internal static void WriteString(BinaryWriter writer, string value)
    {
        var byteCount = Encoding.UTF8.GetByteCount(value);
        if (byteCount > MaxStringUtf8Bytes)
        {
            throw new InvalidDataException(DictionaryResources.PackageStringTooLong);
        }

        writer.Write(byteCount);
        writer.Write(Encoding.UTF8.GetBytes(value));
    }

    internal static string NormalizeInput(string input)
    {
        return string.Join(' ', input.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            .ToLowerInvariant();
    }

    internal static string NormalizeLookupKey(string input)
    {
        return string.Concat(input.Where(character => !char.IsWhiteSpace(character)))
            .ToLowerInvariant();
    }
}
