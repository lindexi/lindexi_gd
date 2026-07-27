using AgentLib.Core.AgentApiManagers.Contexts;

namespace DeepSeekWpf.Models;

public sealed record AgentModelDescriptor(
    string Specifier,
    string Provider,
    string ModelName,
    string? ModelId,
    LlmModelCapabilities? Capabilities,
    int? ContextWindowSize,
    int? MaxOutputTokens);