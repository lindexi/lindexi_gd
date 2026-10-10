using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace CodingChatRoom.AvaloniaShell.Abilities;

internal sealed class AbilityDirectoryReader
{
    private const int MaximumPromptBytes = 1024 * 1024;
    private const int MaximumXmlBytes = 64 * 1024;
    private readonly DirectoryInfo _root;

    public AbilityDirectoryReader(DirectoryInfo root)
    {
        ArgumentNullException.ThrowIfNull(root);
        _root = root;
    }

    public async Task<AbilityCollection> ReadAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _root.Refresh();
        if (!_root.Exists)
        {
            return AbilityCollection.Empty;
        }

        var candidates = new List<AbilityDefinition>();
        foreach (DirectoryInfo directory in _root.EnumerateDirectories())
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                AbilityDefinition? ability = await ReadAbilityAsync(directory, cancellationToken).ConfigureAwait(false);
                if (ability is not null)
                {
                    candidates.Add(ability);
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or XmlException
                                                  or InvalidDataException or CultureNotFoundException)
            {
                Trace.TraceError($"能力目录无效：{directory.FullName}。{exception.Message}");
            }
        }

        HashSet<string> duplicateIds = candidates
            .GroupBy(item => item.Id, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (string duplicateId in duplicateIds)
        {
            Trace.TraceError($"能力 ID 冲突，相关能力均已忽略：{duplicateId}");
        }

        AbilityDefinition? compression = candidates.SingleOrDefault
        (item =>
            !duplicateIds.Contains(item.Id)
            && string.Equals(item.Id, AbilityCatalog.CompressionId, StringComparison.OrdinalIgnoreCase)
        );
        AbilityDefinition[] items = candidates
            .Where
            (item => !duplicateIds.Contains(item.Id)
                     && !string.Equals(item.Id, AbilityCatalog.ProgrammingId, StringComparison.OrdinalIgnoreCase)
                     && !string.Equals(item.Id, AbilityCatalog.CompressionId, StringComparison.OrdinalIgnoreCase)
            )
            .OrderBy(item => item.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(item => item.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return new AbilityCollection
            (items, compression?.Prompt ?? AbilityCatalog.DefaultCompressionPrompt, compression?.DisplayName);
    }

    private static async Task<AbilityDefinition?> ReadAbilityAsync
    (
        DirectoryInfo directory,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        directory.Refresh();
        var configurationFile = new FileInfo(Path.Join(directory.FullName, "Ability.xml"));
        string id;
        string promptFileName;
        string displayName;
        if (configurationFile.Exists)
        {
            (id, promptFileName, displayName) = await ReadConfigurationAsync
            (
                configurationFile,
                directory.Name,
                cancellationToken
            ).ConfigureAwait(false);
        }
        else
        {
            FileInfo[] markdownFiles = directory.EnumerateFiles("*.md", SearchOption.TopDirectoryOnly).ToArray();
            if (markdownFiles.Length != 1)
            {
                return null;
            }

            id = directory.Name;
            ValidateId(id);
            promptFileName = markdownFiles[0].Name;
            displayName = directory.Name;
        }

        var promptFile = new FileInfo(Path.Join(directory.FullName, promptFileName));
        promptFile.Refresh();
        if (!promptFile.Exists || promptFile.Length > MaximumPromptBytes)
        {
            throw new InvalidDataException("提示词文件不存在或超过限制。");
        }

        string prompt = await File.ReadAllTextAsync
        (
            promptFile.FullName,
            new UTF8Encoding(false, true),
            cancellationToken
        ).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(prompt) || !prompt.Contains("{{input}}", StringComparison.Ordinal))
        {
            throw new InvalidDataException("提示词必须非空且包含 {{input}}。");
        }

        return new AbilityDefinition(id, displayName, prompt, directory);
    }

    private static async Task<(string Id, string PromptFileName, string DisplayName)> ReadConfigurationAsync
    (
        FileInfo file,
        string fallbackName,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        file.Refresh();
        if (file.Length > MaximumXmlBytes)
        {
            throw new InvalidDataException("Ability.xml 超过限制。");
        }

        await using FileStream stream = new
        (
            file.FullName,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 4096,
            useAsync: true
        );
        using XmlReader reader = XmlReader.Create
        (
            stream, new XmlReaderSettings
            {
                Async = true,
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
            }
        );
        XDocument document = await XDocument.LoadAsync(reader, LoadOptions.None, cancellationToken).ConfigureAwait
            (false);
        XElement root = document.Root ?? throw new InvalidDataException("缺少 Ability 根节点。");
        if (root.Name.LocalName != "Ability" || root.HasAttributes
                                             || root.Elements().Any
                                             (element => element.Name.LocalName is not ("Id" or "PromptFile"
                                                 or "Names")
                                             ))
        {
            throw new InvalidDataException("Ability.xml 包含未知节点或属性。");
        }

        string id = GetRequiredValue(root, "Id").Trim();
        ValidateId(id);
        string promptFileName = GetRequiredValue(root, "PromptFile").Trim();
        ValidatePromptFileName(promptFileName);
        return (id, promptFileName, ResolveDisplayName(root.Element("Names"), fallbackName));
    }

    private static string ResolveDisplayName(XElement? namesElement, string fallback)
    {
        if (namesElement is null)
        {
            return fallback;
        }

        if (namesElement.HasAttributes || namesElement.Elements().Any(element => element.Name.LocalName != "Name"))
        {
            throw new InvalidDataException("Names 配置无效。");
        }

        var names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string? defaultName = null;
        foreach (XElement element in namesElement.Elements())
        {
            XAttribute[] attributes = element.Attributes().ToArray();
            if (attributes.Length > 1 || (attributes.Length == 1 && attributes[0].Name.LocalName != "Culture"))
            {
                throw new InvalidDataException("Name 属性无效。");
            }

            string value = element.Value.Trim();
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidDataException("Name 不能为空。");
            }

            string? cultureName = element.Attribute("Culture")?.Value.Trim();
            if (cultureName is null)
            {
                if (defaultName is not null) throw new InvalidDataException("默认 Name 重复。");
                defaultName = value;
            }
            else
            {
                CultureInfo culture = CultureInfo.GetCultureInfo(cultureName);
                if (!names.TryAdd(culture.Name, value)) throw new InvalidDataException("Name 语言重复。");
            }
        }

        CultureInfo current = CultureInfo.CurrentUICulture;
        if (names.TryGetValue(current.Name, out string? exact)) return exact;
        if (!current.IsNeutralCulture && names.TryGetValue(current.Parent.Name, out string? parent)) return parent;
        return defaultName ?? fallback;
    }

    private static string GetRequiredValue(XElement root, string name)
    {
        XElement[] elements = root.Elements(name).ToArray();
        if (elements.Length != 1 || elements[0].HasElements || elements[0].HasAttributes)
        {
            throw new InvalidDataException($"{name} 必须且只能出现一次。");
        }

        return elements[0].Value;
    }

    private static void ValidateId(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || id.Any
                (character => char.IsWhiteSpace(character) || char.IsControl(character) || character is '/' or '\\'))
        {
            throw new InvalidDataException("能力 ID 无效。");
        }
    }

    private static void ValidatePromptFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)
            || !string.Equals(fileName, Path.GetFileName(fileName), StringComparison.Ordinal)
            || !string.Equals(Path.GetExtension(fileName), ".md", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("PromptFile 必须是当前目录中的 Markdown 文件名。");
        }
    }
}