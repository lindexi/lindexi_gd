using System.Text.Json;

namespace XiaoXiIme.Cli;

internal sealed record IntegrationPayloadManifest(
    int SchemaVersion,
    DateTimeOffset CreatedAtUtc,
    string RuntimeIdentifier,
    string ImeFile,
    string TsfModule,
    string CliExecutable,
    string ImeHostExecutable,
    string TsfAbiHostExecutable,
    IReadOnlyList<string> TestAssemblies,
    IReadOnlyList<PayloadFile> Files)
{
    public const string FileName = "xiaoxiime-payload.json";

    public static IntegrationPayloadManifest Load(string path)
    {
        var manifest = JsonSerializer.Deserialize<IntegrationPayloadManifest>(File.ReadAllText(path), JsonOptions)
            ?? throw new InvalidDataException($"Invalid payload manifest: {path}");
        return manifest;
    }

    public void Save(string path) => File.WriteAllText(path, JsonSerializer.Serialize(this, JsonOptions));

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };
}

internal sealed record PayloadFile(string Path, long Length, string Sha256);
