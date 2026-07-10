namespace CoursewarePptxGeneratorWpfDemo.Models;

/// <summary>
/// Represents a language model option displayed in the Copilot panel.
/// </summary>
public sealed record CoursewareModelDisplayItem(string Provider, string ModelName)
{
    /// <summary>
    /// Gets the display name used by model selection controls.
    /// </summary>
    public string DisplayName => string.IsNullOrEmpty(Provider) ? ModelName : $"{Provider}: {ModelName}";
}
