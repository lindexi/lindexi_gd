using System.Text;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace XiaoXiIme.Dictionary;

internal sealed class DictionarySourceReader : TextReader
{
    private readonly TextReader reader;
    private string? firstLine;
    private int skippedLines;

    internal DictionarySourceMetadata Metadata { get; private set; } = new(new YamlMappingNode());

    internal DictionarySourceReader(TextReader reader, string filePath)
    {
        ArgumentNullException.ThrowIfNull(reader);
        this.reader = reader;
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith('#'))
            {
                skippedLines++;
                continue;
            }

            if (line.Trim() != "---")
            {
                firstLine = line;
                return;
            }

            skippedLines++;
            var headerStart = skippedLines;
            var yaml = new StringBuilder();
            while ((line = reader.ReadLine()) is not null && line.Trim() != "...")
            {
                skippedLines++;
                yaml.AppendLine(line);
            }

            if (line is null)
            {
                throw new DictionarySourceException(filePath, headerStart, DictionaryResources.InvalidSourceHeader);
            }

            skippedLines++;
            try
            {
                var stream = new YamlStream();
                stream.Load(new StringReader(yaml.ToString()));
                if (stream.Documents.Count != 1 || stream.Documents[0].RootNode is not YamlMappingNode fields)
                {
                    throw new DictionarySourceException(filePath, headerStart, DictionaryResources.InvalidSourceHeader);
                }

                Metadata = new DictionarySourceMetadata(fields);
                foreach (var key in new[] { "name", "version", "description", "sort" })
                {
                    if (fields.Children.TryGetValue(new YamlScalarNode(key), out var value) && value is not YamlScalarNode)
                    {
                        throw new DictionarySourceException(filePath, headerStart, DictionaryResources.InvalidSourceHeader);
                    }
                }

                if (Metadata.Sort is not null and not "by_weight")
                {
                    throw new DictionarySourceException(filePath, headerStart, DictionaryResources.UnsupportedSourceSort);
                }
            }
            catch (YamlException exception)
            {
                throw new DictionarySourceException(filePath, headerStart + (int)exception.Start.Line, DictionaryResources.InvalidSourceHeader);
            }

            return;
        }
    }

    public override string? ReadLine()
    {
        // Preserve physical source line numbers in the existing TSV diagnostics.
        if (skippedLines > 0)
        {
            skippedLines--;
            return string.Empty;
        }

        if (firstLine is not null)
        {
            var line = firstLine;
            firstLine = null;
            return line;
        }

        return reader.ReadLine();
    }
}
