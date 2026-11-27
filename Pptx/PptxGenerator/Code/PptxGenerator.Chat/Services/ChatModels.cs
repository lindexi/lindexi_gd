using System.ClientModel;
using System.ClientModel.Primitives;
using System.IO;
using System.Text.Json;
using AgentLib.Core.AgentApiManagers.Contexts;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;
using OpenAI;
using OpenAI.Responses;

namespace PptxGenerator.Chat.Services;

internal sealed class ChatModel
{
    private readonly ResponsesClient _client;

    internal ChatModel(Uri endpoint, string key, ModelDefinition definition)
    {
        Definition = definition;
        _client = new OpenAIClient(new ApiKeyCredential(key), new OpenAIClientOptions
        {
            Endpoint = endpoint,
            RetryPolicy = new ClientRetryPolicy(0),
        }).GetResponsesClient();
    }

    internal ModelDefinition Definition { get; }
    internal string Id => string.IsNullOrWhiteSpace(Definition.ModelId) ? Definition.ModelName : Definition.ModelId;
    internal bool SupportsTools => Definition.Capabilities?.ToolCall ?? true;
    internal bool SupportsImages => Definition.Capabilities?.Input?.Image ?? true;
    internal ResponsesClient Client => _client;
    public string Label => string.IsNullOrWhiteSpace(Definition.Provider)
        ? Definition.ModelName : $"{Definition.Provider} / {Definition.ModelName}";
}

internal sealed record ChatModels(IReadOnlyList<ChatModel> Options, ChatModel? Primary, string? NoticeKey, string? FailedPath = null)
{
    private const string PreferredPath = @"C:\lindexi\Work\Key\AgentConfiguration.json";

    internal static Task<ChatModels> LoadAsync() =>
        LoadAsync(PreferredPath, Path.Combine(AppContext.BaseDirectory, "AgentConfiguration.json"));

    internal static async Task<ChatModels> LoadAsync(string preferredPath, string fallbackPath)
    {
        string path = preferredPath;
        try
        {
            FileStream stream;
            try
            {
                stream = File.OpenRead(path);
            }
            catch (IOException exception) when (exception is FileNotFoundException or DirectoryNotFoundException)
            {
                path = fallbackPath;
                stream = File.OpenRead(path);
            }

            await using (stream)
            {
                var config = await JsonSerializer.DeserializeAsync<AgentApiManagerConfiguration>(stream, new JsonSerializerOptions
                {
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true,
                }).ConfigureAwait(false);
                if (config is null) throw new JsonException("Expected a model configuration object.");
                var models = new List<ChatModel>();
                int invalid = 0;
                var providers = config.OpenAIConfigurationList ?? Array.Empty<OpenAIProtocolLanguageModelConfiguration>();
                foreach (var provider in providers)
                {
                    if (provider?.ModelDefinitions is not { } definitions) continue;
                    if (!TryHttpUri(provider.EndPoint, out var endpoint) || string.IsNullOrWhiteSpace(provider.Key))
                    {
                        invalid += definitions.Count;
                        continue;
                    }

                    foreach (var model in definitions)
                    {
                        if (model is null || string.IsNullOrWhiteSpace(model.ModelName))
                        {
                            invalid++;
                            continue;
                        }
                        models.Add(new ChatModel(endpoint, provider.Key, model));
                    }
                }

                var matches = models.Where(model => Matches(model, config.PrimaryModel)).Take(2).ToArray();
                var primary = matches.Length == 1 ? matches[0] : null;
                ChatDiagnostics.Info("LoadModels", $"ConfigurationPath={path} ProviderCount={providers.Count} AvailableModelCount={models.Count} InvalidModelCount={invalid} PrimarySelection={(primary is not null ? "Selected" : matches.Length == 0 ? "NoMatch" : "Ambiguous")}");
                return new ChatModels(models, primary, models.Count == 0 ? "NoModels" :
                    primary is null ? "SelectModel" : null);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException or ArgumentException)
        {
            ChatDiagnostics.Failure("LoadModels", exception, "ConfigurationPath", path);
            var reason = exception switch
            {
                FileNotFoundException or DirectoryNotFoundException => "ConfigMissing",
                UnauthorizedAccessException => "ConfigDenied",
                JsonException or ArgumentException => "ConfigInvalid",
                _ => "ConfigIo",
            };
            return new ChatModels(Array.Empty<ChatModel>(), null, reason, path);
        }
    }

    internal static bool TryHttpUri(string? value, out Uri uri)
    {
        if (Uri.TryCreate(value, UriKind.Absolute, out var candidate) &&
            candidate.Scheme is "http" or "https" && candidate.UserInfo.Length == 0)
        {
            uri = candidate;
            return true;
        }
        uri = null!;
        return false;
    }

    private static bool Matches(ChatModel model, string? primary) =>
        !string.IsNullOrWhiteSpace(primary) &&
        (string.Equals(primary, model.Id, StringComparison.OrdinalIgnoreCase) ||
         string.Equals(primary, model.Definition.ModelName, StringComparison.OrdinalIgnoreCase) ||
         string.Equals(primary, $"{model.Definition.Provider}/{model.Definition.ModelName}", StringComparison.OrdinalIgnoreCase));

}
