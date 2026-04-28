using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

using SimpleWrite.Business.SimpleWriteConfigurations;

namespace SimpleWrite.Business.AgentConnectors.CopilotAbilityLoaders;

sealed class CopilotAbilityLoader
{
    public static IEnumerable<CopilotAbilityDefinition> Load(ConfigurationManager configurationManager, List<string> loadErrorList)
    {
        ArgumentNullException.ThrowIfNull(configurationManager);
        ArgumentNullException.ThrowIfNull(loadErrorList);

        string abilityDirectory = configurationManager.GetCopilotAbilityDirectory().Path;
        Directory.CreateDirectory(abilityDirectory);

        foreach (var file in Directory.EnumerateFiles(abilityDirectory, "*.xml", SearchOption.TopDirectoryOnly)
                     .OrderBy(static file => file, StringComparer.OrdinalIgnoreCase))
        {
            CopilotAbilityDefinition? ability = TryParse(file, loadErrorList);
            if (ability is not null)
            {
                yield return ability;
            }
        }
    }

    private static CopilotAbilityDefinition? TryParse(string file, List<string> loadErrorList)
    {
        try
        {
            return Parse(file);
        }
        catch (InvalidOperationException ex)
        {
            loadErrorList.Add($"- {Path.GetFileName(file)}：{ex.Message}");
            return null;
        }
        catch (FormatException ex)
        {
            loadErrorList.Add($"- {Path.GetFileName(file)}：{ex.Message}");
            return null;
        }
        catch (XmlException ex)
        {
            loadErrorList.Add($"- {Path.GetFileName(file)}：XML 格式无效，{ex.Message}");
            return null;
        }
        catch (IOException ex)
        {
            loadErrorList.Add($"- {Path.GetFileName(file)}：读取失败，{ex.Message}");
            return null;
        }
    }

    private static CopilotAbilityDefinition Parse(string file)
    {
        var document = XDocument.Load(file, LoadOptions.PreserveWhitespace);
        XElement root = document.Root ?? throw new InvalidOperationException("XML 缺少根节点。");

        string title = ReadRequiredValue(root, nameof(CopilotAbilityDefinition.Title));
        string content = ReadRequiredValue(root, nameof(CopilotAbilityDefinition.Content));

        if (!content.Contains(CopilotAbilityDefinition.InputPlaceholder, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"`{nameof(CopilotAbilityDefinition.Content)}` 必须包含 `{CopilotAbilityDefinition.InputPlaceholder}` 占位符。");
        }

        int priority = ReadInt32Value(root, nameof(CopilotAbilityDefinition.Priority), defaultValue: 0);
        bool supportSingleLine = ReadBooleanValue(root, nameof(CopilotAbilityDefinition.SupportSingleLine), defaultValue: true);
        return new CopilotAbilityDefinition(title, content, priority, supportSingleLine);
    }

    private static string ReadRequiredValue(XElement root, string propertyName)
    {
        string? value = ReadOptionalValue(root, propertyName);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"缺少 `{propertyName}` 配置。");
        }

        return value.Trim();
    }

    private static int ReadInt32Value(XElement root, string propertyName, int defaultValue)
    {
        string? value = ReadOptionalValue(root, propertyName);
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
        {
            return result;
        }

        throw new FormatException($"`{propertyName}` 必须是整数。");
    }

    private static bool ReadBooleanValue(XElement root, string propertyName, bool defaultValue)
    {
        string? value = ReadOptionalValue(root, propertyName);
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        if (bool.TryParse(value, out bool result))
        {
            return result;
        }

        throw new FormatException($"`{propertyName}` 必须是 `true` 或 `false`。");
    }

    private static string? ReadOptionalValue(XElement root, string propertyName)
    {
        return (string?) root.Attribute(propertyName) ?? root.Element(propertyName)?.Value;
    }
}