using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace CodingChatRoom.AvaloniaShell.Abilities;

internal static class DefaultAbilityInstaller
{
    private const string InitializationMarkerFileName = ".defaults-initialized";

    public static async Task InstallOnceAsync(DirectoryInfo abilitiesDirectory)
    {
        abilitiesDirectory.Create();
        var marker = new FileInfo(Path.Join(abilitiesDirectory.FullName, InitializationMarkerFileName));
        if (marker.Exists)
        {
            return;
        }

        await WriteAbilityAsync
        (
            abilitiesDirectory,
            "CodeReview",
            "code-review",
            "代码审查",
            "Code Review",
            """
            请审查下面的代码、变更或方案。先列出会导致错误、安全问题、数据丢失或兼容性破坏的具体问题，再列出其余可维护性问题。每项都说明影响、依据和最小修改建议；没有发现问题时明确说明，并指出仍需验证的风险。

            {{input}}
            """
        ).ConfigureAwait(false);
        await WriteAbilityAsync
        (
            abilitiesDirectory,
            "Compress",
            AbilityCatalog.CompressionId,
            "按照指令进行压缩",
            "Compress with Instructions",
            """
            请在固定压缩协议之外遵循以下额外要求：

            {{input}}
            """
        ).ConfigureAwait(false);
        await File.WriteAllTextAsync(marker.FullName, string.Empty, new UTF8Encoding(false)).ConfigureAwait(false);
    }

    private static async Task WriteAbilityAsync
    (
        DirectoryInfo root,
        string directoryName,
        string id,
        string chineseName,
        string englishName,
        string prompt
    )
    {
        var directory = new DirectoryInfo(Path.Join(root.FullName, directoryName));
        if (directory.Exists)
        {
            return;
        }

        directory.Create();
        string xml = $"""
                      <Ability>
                        <Id>{id}</Id>
                        <PromptFile>Prompt.md</PromptFile>
                        <Names>
                          <Name Culture="zh-CN">{chineseName}</Name>
                          <Name Culture="en">{englishName}</Name>
                          <Name>{englishName}</Name>
                        </Names>
                      </Ability>
                      """;
        await File.WriteAllTextAsync
            (Path.Join(directory.FullName, "Ability.xml"), xml, new UTF8Encoding(false)).ConfigureAwait(false);
        await File.WriteAllTextAsync
            (Path.Join(directory.FullName, "Prompt.md"), prompt, new UTF8Encoding(false)).ConfigureAwait(false);
    }
}