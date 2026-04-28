using System;

namespace SimpleWrite.Business.AgentConnectors.CopilotAbilityLoaders;

sealed class CopilotAbilityDefinition(string title, string content, int priority, bool supportSingleLine)
{
    public const string InputPlaceholder = "$(Input)";

    public string Title { get; } = title;

    public string Content { get; } = content;

    public int Priority { get; } = priority;

    public bool SupportSingleLine { get; } = supportSingleLine;

    public string CreatePrompt(string input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return Content.Replace(InputPlaceholder, input, StringComparison.Ordinal);
    }
}