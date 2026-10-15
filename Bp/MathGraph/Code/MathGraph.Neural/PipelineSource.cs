using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MathGraphs.Neural;

internal sealed record PipelineItem(long Offset, long Line, int[]? Tokens, bool[]? Labels, string Hash, int Split, bool End);

internal static class PipelineSource
{
    internal static async IAsyncEnumerable<PipelineItem> ReadAsync(PipelineTrainingState state, MiniMindTextTokenizer tokenizer,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await using var file = new FileStream(state.SourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, 8192, FileOptions.Asynchronous);
        file.Position = state.ByteOffset;
        var offset = state.ByteOffset;
        var lineNumber = state.Line;
        using var line = new MemoryStream();
        var buffer = new byte[8192];
        while (state.Options.PrefixLines == 0 || lineNumber < state.Options.PrefixLines)
        {
            var read = await file.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (read == 0) break;
            for (var i = 0; i < read; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                offset++;
                if (offset > state.Limits.MaxFileBytes) throw new InvalidDataException();
                if (buffer[i] == '\n')
                {
                    yield return Parse(line.ToArray(), ++lineNumber, offset, state, tokenizer, cancellationToken);
                    line.SetLength(0);
                    if (state.Options.PrefixLines > 0 && lineNumber >= state.Options.PrefixLines) break;
                }
                else
                {
                    if (line.Length >= state.Limits.MaxLineBytes) throw new InvalidDataException();
                    line.WriteByte(buffer[i]);
                }
            }
        }
        if (line.Length > 0) yield return Parse(line.ToArray(), ++lineNumber, offset, state, tokenizer, cancellationToken);
        yield return new PipelineItem(offset, lineNumber, null, null, string.Empty, 0, true);
    }

    private static PipelineItem Parse(byte[] bytes, long line, long offset, PipelineTrainingState state,
        MiniMindTextTokenizer tokenizer, CancellationToken cancellationToken)
    {
        var text = new UTF8Encoding(false, true).GetString(bytes);
        if (line == 1) text = text.TrimStart('\uFEFF');
        if (string.IsNullOrWhiteSpace(text)) return new PipelineItem(offset, line, null, null, string.Empty, 0, false);
        int[] tokens;
        bool[]? labels = null;
        try
        {
            if (state.Format == "chat")
            {
                var sample = ChatJsonl.ParseSample(text, tokenizer, state.Limits, cancellationToken);
                tokens = sample[0].ToArray();
                labels = sample.GetLabelMask(0)?.ToArray();
            }
            else
            {
                using var json = JsonDocument.Parse(text, new JsonDocumentOptions { MaxDepth = 32 });
                if (json.RootElement.ValueKind != JsonValueKind.Object) throw new JsonException();
                var fields = json.RootElement.EnumerateObject().Where(p => p.NameEquals("text")).ToArray();
                if (fields.Length != 1 || fields[0].Value.ValueKind != JsonValueKind.String) throw new JsonException();
                tokens = tokenizer.Encode(fields[0].Value.GetString()!, state.Limits.MaxSampleTokens - 1).Append(tokenizer.EosTokenId).ToArray();
                if (tokens.Length < 2) throw new JsonException();
            }
        }
        catch (JsonException error)
        {
            throw new InvalidDataException(string.Format(System.Globalization.CultureInfo.CurrentCulture,
                Numeric.Message(state.Format == "chat" ? "ChatJsonlInvalid" : "JsonlInvalid"), line), error);
        }
        var content = new byte[checked(tokens.Length * 4 + 8)];
        BinaryPrimitives.WriteUInt64LittleEndian(content, state.Partition.Seed);
        for (var i = 0; i < tokens.Length; i++) BinaryPrimitives.WriteInt32LittleEndian(content.AsSpan(8 + i * 4), tokens[i]);
        var hash = SHA256.HashData(content);
        var draw = (BinaryPrimitives.ReadUInt64LittleEndian(hash) >> 11) * (1.0 / (1UL << 53));
        var split = draw < state.Partition.StructureSelectionFraction ? 1 :
            draw < state.Partition.StructureSelectionFraction + state.Partition.TestFraction ? 2 : 0;
        return new PipelineItem(offset, line, tokens, labels, Convert.ToHexString(hash), split, false);
    }
}
