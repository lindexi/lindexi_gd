using System.Collections.Generic;

namespace SimpleWrite.Business.Snippets;

/// <summary>
/// 代码片管理器
/// </summary>
public class SnippetManager
{
    public SnippetManager()
    {
        // 以下是两个内置的代码片，用于快速接入调试
        AddSnippet(new Snippet()
        {
            TriggerText = "ck",
            ContentText =
                """
                ```csharp
                
                ```
                """,
            RelativeCaretOffset = "```csharp\n".Length
        });

        AddSnippet(new Snippet()
        {
            TriggerText = "cx",
            ContentText =
                """
                ```xml
                
                ```
                """,
            RelativeCaretOffset = "```xml\n".Length
        });
    }

    private readonly List<Snippet> _snippets = new();
    public void AddSnippet(Snippet snippet)
    {
        _snippets.Add(snippet);
    }
    public IEnumerable<Snippet> GetSnippets()
    {
        return _snippets;
    }

    public Snippet? Match(string triggerText)
    {
        foreach (var snippet in _snippets)
        {
            if (snippet.TriggerText == triggerText)
            {
                return snippet;
            }
        }
        return null;
    }
}