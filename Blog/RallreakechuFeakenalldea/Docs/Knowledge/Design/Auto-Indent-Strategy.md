# 回车自动缩进策略

## 场景

`SimpleWriteTextEditor` 按下回车换行时，根据当前文档的语言类型和当前行内容，自动决定下一行的缩进空格数。

具体行为：

- **Markdown / 纯文本**：复制当前行前导空格到下一行。
- **XML / XAML / HTML / SVG**：复制当前行前导空格，并根据光标前的标签嵌套层级（开标签数 - 闭标签数）增加或减少缩进。
- 光标在行中间时，仅分析光标之前的文本内容来决定缩进。

## 架构

```
SimpleWriteTextEditorHandler.BreakLine()
  └─ AutoIndentStrategySelector.GetStrategy(definition)
       ├─ MarkdownAutoIndentStrategy  → 纯空格复制
       └─ XmlAutoIndentStrategy      → 空格复制 + 标签嵌套调整
```

### 策略接口

`IAutoIndentStrategy` 定义在 `SimpleWrite/Business/TextEditors/AutoIndentStrategies/`：

```csharp
public interface IAutoIndentStrategy
{
    string GetIndentText(string currentLineText, int caretColumnInLine);
}
```

- `currentLineText`：当前行完整文本（不含换行符）。
- `caretColumnInLine`：光标在当前行中的列位置（0-based）。光标在行尾时等于文本长度。
- 返回值：要插入到新行开头的缩进字符串（不含换行符）。

### 策略选择器

`AutoIndentStrategySelector` 根据 `DocumentHighlightDefinition` 选择策略：

- `DocumentHighlightCategory.Markdown` / `CSharp` → `MarkdownAutoIndentStrategy`
- `DocumentHighlightCategory.Other` + `LanguageId` 为 `xml`/`xaml`/`html`/`svg` → `XmlAutoIndentStrategy`
- 其他 → `MarkdownAutoIndentStrategy`

## 实现约定

### MarkdownAutoIndentStrategy

1. 统计当前行开头的连续空白字符（空格或 Tab）。
2. 取 `Math.Min(leadingCount, caretColumnInLine)` 作为实际复制的空白字符数。
3. 如果光标之前没有空白字符，返回空字符串。

### XmlAutoIndentStrategy

1. 默认缩进增量 `IndentSize` 为 4，可通过构造函数配置。
2. 统计当前行前导空白字符数，同样受 `caretColumnInLine` 约束。
3. 分析光标之前文本中的 XML 标签：
   - 遇到 `<tag>`（开标签）→ `netLevel++`
   - 遇到 `</tag>`（闭标签）→ `netLevel--`
   - 遇到 `<?...?>` 或 `<!--...-->` → 跳过
   - 遇到 `<tag />`（自闭合）→ 不计入
4. 最终缩进 = `effectiveLeadingCount + netLevel * IndentSize`，最小为 0。

## 开关控制

`SimpleWriteTextEditor.IsAutoIndentEnabled`（默认 `true`）控制是否启用自动缩进。设为 `false` 时，回车行为退化为普通换行。

## 关键文件

| 文件 | 职责 |
|---|---|
| `SimpleWrite/Business/TextEditors/AutoIndentStrategies/IAutoIndentStrategy.cs` | 策略接口 |
| `SimpleWrite/Business/TextEditors/AutoIndentStrategies/MarkdownAutoIndentStrategy.cs` | Markdown 缩进策略 |
| `SimpleWrite/Business/TextEditors/AutoIndentStrategies/XmlAutoIndentStrategy.cs` | XML 缩进策略 |
| `SimpleWrite/Business/TextEditors/AutoIndentStrategies/AutoIndentStrategySelector.cs` | 策略选择器 |
| `SimpleWrite/Business/TextEditors/SimpleWriteTextEditor.cs` | `IsAutoIndentEnabled`、`DocumentHighlightDefinition` |
| `SimpleWrite/Business/TextEditors/SimpleWriteTextEditorHandler.cs` | `BreakLine()` 重写 |

## 扩展指南

新增语言支持时：

1. 实现 `IAutoIndentStrategy` 接口。
2. 在 `AutoIndentStrategySelector.LanguageStrategyMap` 中添加 `LanguageId` 到策略的映射，或为其 `DocumentHighlightCategory` 添加分支。
3. 无需修改 `SimpleWriteTextEditorHandler` 或 `SimpleWriteTextEditor`。

## 适用场景

- 编写 Markdown 文档时保持缩进层级
- 编写 XML/XAML/HTML 时自动管理标签缩进
- 后续扩展 C#、JSON 等语言的智能缩进