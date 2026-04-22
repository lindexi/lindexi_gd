using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LightTextEditorPlus;
using SimpleWrite.Business.TextEditors.CommandPatterns;

namespace SimpleWrite.Business.PluginCommandPatterns;

sealed class RunCommandLineCommandPattern : ICommandPattern
{
    private static readonly char[] LeadingInvalidStartCharacters =
    [
        '>',
        '!',
        '#',
        '|',
        '&',
        ';',
        ',',
        ')',
        '(',
        '[',
        ']',
        '{',
        '}',
        '~',
        '<',
        '*',
        '?',
    ];

    private static readonly string[] WindowsExecutableExtensions = [".exe", ".bat", ".cmd", ".com"];

    private static readonly HashSet<string> WindowsBuiltInCommandSet = new(StringComparer.OrdinalIgnoreCase)
    {
        "assoc",
        "break",
        "call",
        "cd",
        "chdir",
        "cls",
        "copy",
        "date",
        "del",
        "dir",
        "echo",
        "endlocal",
        "erase",
        "exit",
        "for",
        "ftype",
        "goto",
        "if",
        "md",
        "mkdir",
        "mklink",
        "move",
        "path",
        "pause",
        "popd",
        "prompt",
        "pushd",
        "rd",
        "ren",
        "rename",
        "rmdir",
        "set",
        "setlocal",
        "start",
        "time",
        "title",
        "type",
        "ver",
        "vol",
    };

    private static readonly HashSet<string> UnixBuiltInCommandSet = new(StringComparer.Ordinal)
    {
        "alias",
        "cd",
        "echo",
        "eval",
        "exec",
        "exit",
        "export",
        "pwd",
        "set",
        "source",
        "unalias",
        "unset",
    };

    private readonly PlatformTerminalRunner? _platformTerminalRunner = PlatformTerminalRunner.CreateCurrent();

    public bool SupportSingleLine => true;

    public ValueTask<bool> IsMatchAsync(string text)
    {
        if (_platformTerminalRunner is null)
        {
            return ValueTask.FromResult(false);
        }

        if (!TryNormalizeCommandText(text, out string commandText))
        {
            return ValueTask.FromResult(false);
        }

        string? firstCommandLine = GetFirstCommandLine(commandText);
        if (string.IsNullOrWhiteSpace(firstCommandLine))
        {
            return ValueTask.FromResult(false);
        }

        if (!TryGetFirstToken(firstCommandLine, out string firstToken))
        {
            return ValueTask.FromResult(false);
        }

        if (!LooksLikeValidCommandToken(firstToken))
        {
            return ValueTask.FromResult(false);
        }

        if (TryResolveCommandPath(firstToken, out _))
        {
            return ValueTask.FromResult(true);
        }

        if (IsBuiltInCommand(firstToken))
        {
            return ValueTask.FromResult(true);
        }

        if (OperatingSystem.IsWindows())
        {
            string extension = Path.GetExtension(TrimWrappingQuotes(firstToken));
            return ValueTask.FromResult(WindowsExecutableExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase));
        }

        return ValueTask.FromResult(false);
    }

    public string Title => "在终端运行";

    public async Task DoAsync(string text, TextEditor textEditor)
    {
        _ = textEditor;

        if (_platformTerminalRunner is null)
        {
            return;
        }

        if (!TryNormalizeCommandText(text, out string commandText))
        {
            return;
        }

        await _platformTerminalRunner.ExecuteAsync(commandText);
    }

    private static bool TryNormalizeCommandText(string text, out string commandText)
    {
        commandText = text.Trim();
        if (string.IsNullOrWhiteSpace(commandText))
        {
            commandText = string.Empty;
            return false;
        }

        if (commandText.Length >= 2
            && commandText.StartsWith('`')
            && commandText.EndsWith('`')
            && commandText.IndexOf('`', 1) == commandText.Length - 1)
        {
            commandText = commandText[1..^1].Trim();
        }

        if (TryUnwrapMarkdownCodeFence(commandText, out string fencedText))
        {
            commandText = fencedText;
        }

        commandText = NormalizePromptLines(commandText);
        return !string.IsNullOrWhiteSpace(commandText);
    }

    private static bool TryUnwrapMarkdownCodeFence(string text, out string fencedText)
    {
        fencedText = string.Empty;

        string normalizedText = text.Replace("\r\n", "\n");
        string[] lines = normalizedText.Split('\n');
        if (lines.Length < 3)
        {
            return false;
        }

        if (!lines[0].TrimStart().StartsWith("```", StringComparison.Ordinal) || lines[^1].Trim() != "```")
        {
            return false;
        }

        fencedText = string.Join(Environment.NewLine, lines.Skip(1).Take(lines.Length - 2)).Trim();
        return !string.IsNullOrWhiteSpace(fencedText);
    }

    private static string NormalizePromptLines(string text)
    {
        using var reader = new StringReader(text);
        var writer = new StringWriter();
        string? line;
        bool isFirstLine = true;
        while ((line = reader.ReadLine()) is not null)
        {
            string normalizedLine = line.Trim();
            if (string.IsNullOrWhiteSpace(normalizedLine))
            {
                continue;
            }

            if (normalizedLine.StartsWith("$ ", StringComparison.Ordinal))
            {
                normalizedLine = normalizedLine[2..].TrimStart();
            }
            else if (TryTrimWindowsPrompt(normalizedLine, out string promptText))
            {
                normalizedLine = promptText;
            }

            if (!isFirstLine)
            {
                writer.WriteLine();
            }

            writer.Write(normalizedLine);
            isFirstLine = false;
        }

        return writer.ToString().Trim();
    }

    private static bool TryTrimWindowsPrompt(string text, out string commandText)
    {
        commandText = string.Empty;

        int separatorIndex = text.IndexOf('>');
        if (separatorIndex <= 1)
        {
            return false;
        }

        string promptPart = text[..separatorIndex];
        if (!promptPart.Contains(':', StringComparison.Ordinal))
        {
            return false;
        }

        commandText = text[(separatorIndex + 1)..].TrimStart();
        return !string.IsNullOrWhiteSpace(commandText);
    }

    private static string? GetFirstCommandLine(string text)
    {
        using var reader = new StringReader(text);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            string trimmedLine = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmedLine))
            {
                continue;
            }

            return trimmedLine;
        }

        return null;
    }

    private static bool TryGetFirstToken(string commandLine, out string token)
    {
        token = string.Empty;

        string trimmedCommandLine = commandLine.TrimStart();
        if (string.IsNullOrWhiteSpace(trimmedCommandLine))
        {
            return false;
        }

        if (trimmedCommandLine[0] == '"')
        {
            int quoteEndIndex = trimmedCommandLine.IndexOf('"', 1);
            if (quoteEndIndex <= 1)
            {
                return false;
            }

            token = trimmedCommandLine[..(quoteEndIndex + 1)];
            return true;
        }

        int separatorIndex = trimmedCommandLine.IndexOfAny([' ', '\t']);
        token = separatorIndex < 0 ? trimmedCommandLine : trimmedCommandLine[..separatorIndex];
        return !string.IsNullOrWhiteSpace(token);
    }

    private static bool LooksLikeValidCommandToken(string token)
    {
        string normalizedToken = TrimWrappingQuotes(token);
        if (string.IsNullOrWhiteSpace(normalizedToken))
        {
            return false;
        }

        if (LeadingInvalidStartCharacters.Contains(normalizedToken[0]))
        {
            return false;
        }

        if (normalizedToken is "." or "..")
        {
            return false;
        }

        if (normalizedToken.Contains('=')
            && !normalizedToken.Contains(Path.DirectorySeparatorChar)
            && !normalizedToken.Contains(Path.AltDirectorySeparatorChar))
        {
            return false;
        }

        return true;
    }

    private static bool TryResolveCommandPath(string token, out string? commandPath)
    {
        commandPath = null;

        string normalizedToken = TrimWrappingQuotes(token);
        if (string.IsNullOrWhiteSpace(normalizedToken))
        {
            return false;
        }

        if (LooksLikePath(normalizedToken))
        {
            return TryResolvePathCandidate(normalizedToken, out commandPath);
        }

        return TryResolveCommandFromPath(normalizedToken, out commandPath);
    }

    private static bool LooksLikePath(string token)
    {
        return Path.IsPathRooted(token)
               || token.Contains(Path.DirectorySeparatorChar)
               || token.Contains(Path.AltDirectorySeparatorChar)
               || token.StartsWith(".", StringComparison.Ordinal);
    }

    private static bool TryResolvePathCandidate(string token, out string? commandPath)
    {
        commandPath = null;

        string fullPath = Path.GetFullPath(token, Environment.CurrentDirectory);
        if (IsExecutableCommandFile(fullPath))
        {
            commandPath = fullPath;
            return true;
        }

        if (!Path.HasExtension(fullPath))
        {
            foreach (string extension in GetCommandExtensions())
            {
                string candidate = fullPath + extension;
                if (IsExecutableCommandFile(candidate))
                {
                    commandPath = candidate;
                    return true;
                }
            }
        }

        return false;
    }

    private static bool TryResolveCommandFromPath(string commandName, out string? commandPath)
    {
        commandPath = null;

        string? path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        string[] commandExtensions = GetCommandExtensions();
        foreach (string folder in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
            {
                continue;
            }

            if (Path.HasExtension(commandName))
            {
                string filePath = Path.Combine(folder, commandName);
                if (IsExecutableCommandFile(filePath))
                {
                    commandPath = filePath;
                    return true;
                }

                continue;
            }

            foreach (string extension in commandExtensions)
            {
                string filePath = Path.Combine(folder, commandName + extension);
                if (IsExecutableCommandFile(filePath))
                {
                    commandPath = filePath;
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsExecutableCommandFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return false;
        }

        if (OperatingSystem.IsWindows())
        {
            return true;
        }

        UnixFileMode unixFileMode = File.GetUnixFileMode(filePath);
        return (unixFileMode & (UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute)) != 0;
    }

    private static bool IsBuiltInCommand(string token)
    {
        string normalizedToken = TrimWrappingQuotes(token);

        if (OperatingSystem.IsWindows())
        {
            return WindowsBuiltInCommandSet.Contains(normalizedToken);
        }

        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            return UnixBuiltInCommandSet.Contains(normalizedToken);
        }

        return false;
    }

    private static string[] GetCommandExtensions()
    {
        if (OperatingSystem.IsWindows())
        {
            string? pathext = Environment.GetEnvironmentVariable("PATHEXT");
            if (!string.IsNullOrWhiteSpace(pathext))
            {
                return pathext.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            }

            return WindowsExecutableExtensions;
        }

        return [string.Empty];
    }

    private static string TrimWrappingQuotes(string text)
    {
        if (text.Length >= 2 && text[0] == '"' && text[^1] == '"')
        {
            return text[1..^1];
        }

        return text;
    }
}
