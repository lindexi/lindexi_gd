using System;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;

namespace PrismCodingAgent.AvaloniaShell.Services.Settings;

internal static class DeepSeekApiKeyResolver
{
    internal static string? Resolve(string? apiKey, AgentApiManagerConfiguration? configuration)
    {
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            return apiKey.Trim();
        }

        if (configuration?.OpenAIConfigurationList is not { } services)
        {
            return null;
        }

        foreach (var service in services)
        {
            // Restrict credential reuse to the official HTTPS origin, never a provider or model name.
            if (!string.IsNullOrWhiteSpace(service.Key)
                && Uri.TryCreate(service.EndPoint, UriKind.Absolute, out var endpoint)
                && endpoint.Scheme == Uri.UriSchemeHttps
                && string.Equals(endpoint.Host, "api.deepseek.com", StringComparison.OrdinalIgnoreCase)
                && endpoint.IsDefaultPort
                && endpoint.UserInfo.Length == 0)
            {
                return service.Key.Trim();
            }
        }

        return null;
    }
}
