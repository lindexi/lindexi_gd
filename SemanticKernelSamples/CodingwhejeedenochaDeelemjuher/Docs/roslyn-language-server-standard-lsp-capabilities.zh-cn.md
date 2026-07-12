# roslyn-language-server 标准 LSP 能力输入输出参考

本文详细说明 `roslyn-language-server` 当前公开的主要标准 Language Server Protocol（LSP）能力，包括客户端发送的 JSON-RPC 方法、`params` 输入格式、服务器 `result` 输出格式、能力协商条件和常见调用示例。

本文只覆盖标准 LSP 能力。Roslyn 的 Visual Studio 扩展协议和 `solution/open`、`project/open` 等自定义方法不在本文范围内。

## 阅读约定

### 消息帧

本文示例只展示 JSON 正文。通过 stdio 或命名管道实际传输时，每条消息仍须使用 LSP 消息帧：

```text
Content-Length: <JSON 正文的 UTF-8 字节数>\r\n
\r\n
<JSON 正文>
```

### 请求、响应与通知

请求包含 `id`，服务器返回同一 `id` 的响应：

```json
{
  "jsonrpc": "2.0",
  "id": 10,
  "method": "textDocument/hover",
  "params": {}
}
```

```json
{
  "jsonrpc": "2.0",
  "id": 10,
  "result": null
}
```

通知不包含 `id`，也没有对应响应，例如 `textDocument/didOpen`。

### 位置、范围和 URI

LSP 的行号和字符位置均从 `0` 开始：

```json
{
  "line": 4,
  "character": 12
}
```

`character` 按 UTF-16 代码单元计数，不应直接按 UTF-8 字节或 Unicode 标量计数。范围采用左闭右开形式，`end` 指向范围结束后的第一个位置：

```json
{
  "start": { "line": 4, "character": 8 },
  "end": { "line": 4, "character": 13 }
}
```

本地文件使用绝对文件 URI：

```text
file:///C:/Code/MyRepo/Program.cs
```

### 文档状态

除工作区符号等少数功能外，大多数请求都依赖服务器当前维护的文档快照。客户端应先发送 `textDocument/didOpen`，编辑后发送递增版本号的 `textDocument/didChange`，再发起语言功能请求。

### 可空结果

许多方法在当前位置没有可用结果时返回 `null`，也有部分方法返回空数组。客户端应同时正确处理 `null` 和 `[]`，不要假定必然存在结果。

### 不透明 data

补全项、代码操作、CodeLens、Inlay Hint、调用层次结构项和类型层次结构项中的 `data` 是服务器内部状态。本文示例中的 `data` 只表示该字段存在，不展示其真实结构。客户端不得自行构造、删减或解释它；进行 resolve 或展开请求时，必须把初始响应中的完整对象原样传回。

## 能力与方法速查

| 功能 | 主要方法 | 输入 | 输出 |
|------|----------|------|------|
| 补全 | `textDocument/completion` | 文档、位置、触发上下文 | `CompletionList`、`CompletionItem[]` 或 `null` |
| 补全解析 | `completionItem/resolve` | 原补全项 | 补充后的 `CompletionItem` |
| 签名帮助 | `textDocument/signatureHelp` | 文档、位置、触发上下文 | `SignatureHelp` 或 `null` |
| 悬停 | `textDocument/hover` | 文档和位置 | `Hover` 或 `null` |
| 定义/类型定义/实现 | `textDocument/definition` 等 | 文档和位置 | `Location`、`Location[]`、`LocationLink[]` 或 `null` |
| 引用 | `textDocument/references` | 文档、位置和引用上下文 | `Location[]` 或 `null` |
| 文档高亮 | `textDocument/documentHighlight` | 文档和位置 | `DocumentHighlight[]` 或 `null` |
| 文档符号 | `textDocument/documentSymbol` | 文档 | `DocumentSymbol[]`、`SymbolInformation[]` 或 `null` |
| 工作区符号 | `workspace/symbol` | 查询文本 | `WorkspaceSymbol[]`、`SymbolInformation[]` 或 `null` |
| 重命名 | `textDocument/prepareRename`、`textDocument/rename` | 文档、位置、新名称 | 范围或 `WorkspaceEdit` |
| 代码操作 | `textDocument/codeAction`、`codeAction/resolve` | 文档、范围、诊断上下文 | `CodeAction[]`/`Command[]` 或补充后的 `CodeAction` |
| 格式化 | `textDocument/formatting` 等 | 文档、范围或输入字符、格式选项 | `TextEdit[]` 或 `null` |
| 折叠范围 | `textDocument/foldingRange` | 文档 | `FoldingRange[]` 或 `null` |
| 选择范围 | `textDocument/selectionRange` | 文档和多个位置 | `SelectionRange[]` 或 `null` |
| 调用层次结构 | `textDocument/prepareCallHierarchy` 等 | 位置或层次结构项 | 调用项数组或 `null` |
| 类型层次结构 | `textDocument/prepareTypeHierarchy` 等 | 位置或层次结构项 | 类型项数组或 `null` |
| 语义标记 | `textDocument/semanticTokens/range` 或 `/full` | 文档及可选范围 | `SemanticTokens` 或 `null` |
| CodeLens | `textDocument/codeLens`、`codeLens/resolve` | 文档或原 CodeLens | `CodeLens[]` 或补充后的 `CodeLens` |
| Inlay Hint | `textDocument/inlayHint`、`inlayHint/resolve` | 文档和范围或原提示 | `InlayHint[]` 或补充后的 `InlayHint` |
| 文档同步 | `textDocument/didOpen` 等 | 文档内容或增量变化 | 通知，无响应 |
| 文件重命名 | `workspace/willRenameFiles` | 新旧文件 URI | `WorkspaceEdit` 或 `null` |
| Pull diagnostics | `textDocument/diagnostic`、`workspace/diagnostic` | 文档或工作区诊断状态 | 诊断报告 |

## 代码补全

服务器能力：

```json
{
  "completionProvider": {
    "resolveProvider": true,
    "triggerCharacters": ["."],
    "allCommitCharacters": [";", "."]
  }
}
```

实际触发字符和提交字符由当前加载的 C#、Visual Basic 补全提供程序汇总，客户端应读取 `initialize` 响应，不能依赖上面的简化示例固定不变。

### textDocument/completion

输入 `CompletionParams` 的主要字段：

- `textDocument.uri`：目标文档 URI。
- `position`：补全位置。
- `context.triggerKind`：`1` 表示主动调用，`2` 表示触发字符，`3` 表示补全不完整后的再次触发。
- `context.triggerCharacter`：由字符触发时使用。

请求示例：

```json
{
  "jsonrpc": "2.0",
  "id": 20,
  "method": "textDocument/completion",
  "params": {
    "textDocument": {
      "uri": "file:///C:/Code/MyRepo/Program.cs"
    },
    "position": {
      "line": 8,
      "character": 16
    },
    "context": {
      "triggerKind": 2,
      "triggerCharacter": "."
    }
  }
}
```

输出可以是 `CompletionList`、补全项数组或 `null`。Roslyn 通常可以返回带 `items` 的补全列表：

```json
{
  "jsonrpc": "2.0",
  "id": 20,
  "result": {
    "isIncomplete": false,
    "items": [
      {
        "label": "WriteLine",
        "kind": 2,
        "sortText": "WriteLine",
        "filterText": "WriteLine",
        "insertText": "WriteLine",
        "data": "<服务器返回的不透明数据>"
      }
    ]
  }
}
```

`CompletionItem.kind` 是协议枚举数值，例如 `2` 表示方法。客户端应保留服务器返回的 `data`，因为 resolve 请求需要用它找回原补全上下文。

### completionItem/resolve

客户端选择或聚焦某个补全项时，可以把该项原样发回服务器，以延迟获取文档、详细信息或附加文本编辑。下例的占位字符串不可直接发送，必须替换为初始响应中的真实 `data`；更可靠的做法是直接复用完整补全项对象。

```json
{
  "jsonrpc": "2.0",
  "id": 21,
  "method": "completionItem/resolve",
  "params": {
    "label": "WriteLine",
    "kind": 2,
    "sortText": "WriteLine",
    "filterText": "WriteLine",
    "insertText": "WriteLine",
    "data": "<服务器返回的不透明数据>"
  }
}
```

响应仍是 `CompletionItem`，但可能增加 `detail`、`documentation`、`textEdit` 或 `additionalTextEdits`：

```json
{
  "jsonrpc": "2.0",
  "id": 21,
  "result": {
    "label": "WriteLine",
    "kind": 2,
    "detail": "void Console.WriteLine(string? value)",
    "documentation": {
      "kind": "markdown",
      "value": "将指定的字符串值写入标准输出流。"
    },
    "insertText": "WriteLine",
    "data": "<服务器返回的不透明数据>"
  }
}
```

客户端不能删除或重建不认识的字段；应将原项目完整传回。

## 签名帮助

服务器在 `signatureHelpProvider` 中声明触发和重新触发字符，具体集合取决于语言服务提供程序。

`SignatureHelpParams.context` 是标准 LSP 输入的一部分。当前 Roslyn 处理器接受该字段，但计算时按主动调用处理，不使用 `triggerKind`、`triggerCharacter` 或 `isRetrigger` 改变签名帮助结果。

### textDocument/signatureHelp

请求示例：

```json
{
  "jsonrpc": "2.0",
  "id": 30,
  "method": "textDocument/signatureHelp",
  "params": {
    "textDocument": {
      "uri": "file:///C:/Code/MyRepo/Program.cs"
    },
    "position": {
      "line": 10,
      "character": 25
    },
    "context": {
      "triggerKind": 2,
      "triggerCharacter": ",",
      "isRetrigger": false
    }
  }
}
```

输出 `SignatureHelp` 包含候选签名、当前签名索引和当前参数索引：

```json
{
  "jsonrpc": "2.0",
  "id": 30,
  "result": {
    "signatures": [
      {
        "label": "string string.Substring(int startIndex, int length)",
        "documentation": {
          "kind": "markdown",
          "value": "从此实例检索子字符串。"
        },
        "parameters": [
          { "label": "int startIndex" },
          { "label": "int length" }
        ]
      }
    ],
    "activeSignature": 0,
    "activeParameter": 1
  }
}
```

没有适用调用时返回 `null`。

## 悬停信息

### textDocument/hover

输入是文档 URI 和光标位置：

```json
{
  "jsonrpc": "2.0",
  "id": 40,
  "method": "textDocument/hover",
  "params": {
    "textDocument": {
      "uri": "file:///C:/Code/MyRepo/Program.cs"
    },
    "position": {
      "line": 5,
      "character": 12
    }
  }
}
```

输出 `Hover` 的 `contents` 可使用 `MarkupContent`。具体使用 Markdown 还是纯文本受客户端 `textDocument.hover.contentFormat` 影响：

```json
{
  "jsonrpc": "2.0",
  "id": 40,
  "result": {
    "contents": {
      "kind": "markdown",
      "value": "```csharp\nclass Customer\n```\n\n表示客户。"
    },
    "range": {
      "start": { "line": 5, "character": 8 },
      "end": { "line": 5, "character": 16 }
    }
  }
}
```

当前位置没有可悬停内容时返回 `null`。

## 定义、类型定义与实现

三个方法输入形状相同：

- `textDocument/definition`
- `textDocument/typeDefinition`
- `textDocument/implementation`

请求示例：

```json
{
  "jsonrpc": "2.0",
  "id": 50,
  "method": "textDocument/definition",
  "params": {
    "textDocument": {
      "uri": "file:///C:/Code/MyRepo/Program.cs"
    },
    "position": {
      "line": 14,
      "character": 18
    }
  }
}
```

常见输出是一个或多个 `Location`：

```json
{
  "jsonrpc": "2.0",
  "id": 50,
  "result": [
    {
      "uri": "file:///C:/Code/MyRepo/Customer.cs",
      "range": {
        "start": { "line": 2, "character": 13 },
        "end": { "line": 2, "character": 21 }
      }
    }
  ]
}
```

LSP 3.17 协议还允许单个 `Location` 或 `LocationLink[]`。当前 Roslyn 的标准定义、类型定义和实现处理器实际返回 `Location[]`；即使客户端声明相应 `linkSupport`，也不应依赖收到 `LocationLink`。下面仅展示协议允许的 `LocationLink` 形状：

```json
{
  "originSelectionRange": {
    "start": { "line": 14, "character": 16 },
    "end": { "line": 14, "character": 24 }
  },
  "targetUri": "file:///C:/Code/MyRepo/Customer.cs",
  "targetRange": {
    "start": { "line": 2, "character": 0 },
    "end": { "line": 8, "character": 1 }
  },
  "targetSelectionRange": {
    "start": { "line": 2, "character": 13 },
    "end": { "line": 2, "character": 21 }
  }
}
```

没有目标时返回 `null`。

## 查找引用

服务器将 `referencesProvider.workDoneProgress` 声明为 `true`，客户端可提供 `workDoneToken` 接收进度。

### textDocument/references

`context.includeDeclaration` 决定结果是否包含声明位置：

```json
{
  "jsonrpc": "2.0",
  "id": 60,
  "method": "textDocument/references",
  "params": {
    "textDocument": {
      "uri": "file:///C:/Code/MyRepo/Customer.cs"
    },
    "position": {
      "line": 2,
      "character": 15
    },
    "context": {
      "includeDeclaration": true
    },
    "workDoneToken": "references-1",
    "partialResultToken": "references-partial-1"
  }
}
```

最终输出为 `Location[]` 或 `null`：

```json
{
  "jsonrpc": "2.0",
  "id": 60,
  "result": [
    {
      "uri": "file:///C:/Code/MyRepo/Customer.cs",
      "range": {
        "start": { "line": 2, "character": 13 },
        "end": { "line": 2, "character": 21 }
      }
    },
    {
      "uri": "file:///C:/Code/MyRepo/Program.cs",
      "range": {
        "start": { "line": 14, "character": 16 },
        "end": { "line": 14, "character": 24 }
      }
    }
  ]
}
```

使用 partial result 时，服务器还可以通过 `$/progress` 分批发送位置；客户端应合并部分结果和最终结果，并正确处理取消请求。

## 文档高亮

### textDocument/documentHighlight

```json
{
  "jsonrpc": "2.0",
  "id": 70,
  "method": "textDocument/documentHighlight",
  "params": {
    "textDocument": {
      "uri": "file:///C:/Code/MyRepo/Program.cs"
    },
    "position": {
      "line": 7,
      "character": 10
    }
  }
}
```

输出只针对当前文档。`kind` 常见值为 `1`（Text）、`2`（Read）或 `3`（Write）：

```json
{
  "jsonrpc": "2.0",
  "id": 70,
  "result": [
    {
      "range": {
        "start": { "line": 7, "character": 8 },
        "end": { "line": 7, "character": 13 }
      },
      "kind": 3
    },
    {
      "range": {
        "start": { "line": 10, "character": 20 },
        "end": { "line": 10, "character": 25 }
      },
      "kind": 2
    }
  ]
}
```

## 文档符号

### textDocument/documentSymbol

```json
{
  "jsonrpc": "2.0",
  "id": 80,
  "method": "textDocument/documentSymbol",
  "params": {
    "textDocument": {
      "uri": "file:///C:/Code/MyRepo/Customer.cs"
    }
  }
}
```

客户端支持层次化文档符号时，服务器可返回 `DocumentSymbol[]`：

```json
{
  "jsonrpc": "2.0",
  "id": 80,
  "result": [
    {
      "name": "Customer",
      "kind": 5,
      "range": {
        "start": { "line": 2, "character": 0 },
        "end": { "line": 12, "character": 1 }
      },
      "selectionRange": {
        "start": { "line": 2, "character": 13 },
        "end": { "line": 2, "character": 21 }
      },
      "children": [
        {
          "name": "Name",
          "kind": 7,
          "range": {
            "start": { "line": 4, "character": 4 },
            "end": { "line": 4, "character": 36 }
          },
          "selectionRange": {
            "start": { "line": 4, "character": 18 },
            "end": { "line": 4, "character": 22 }
          }
        }
      ]
    }
  ]
}
```

不支持层次化结果的客户端可能得到扁平的 `SymbolInformation[]`，其中每一项使用 `location` 和可选 `containerName`。

## 工作区符号

### workspace/symbol

输入 `query` 是客户端提供的搜索文本。空字符串可用于请求全部符号，但大型工作区中开销可能很高。

```json
{
  "jsonrpc": "2.0",
  "id": 90,
  "method": "workspace/symbol",
  "params": {
    "query": "Customer"
  }
}
```

LSP 3.17 允许输出 `WorkspaceSymbol[]` 或旧版 `SymbolInformation[]`。当前 Roslyn 处理器实际生成 `SymbolInformation[]`：

```json
{
  "jsonrpc": "2.0",
  "id": 90,
  "result": [
    {
      "name": "Customer",
      "kind": 5,
      "containerName": "MyRepo.Models",
      "location": {
        "uri": "file:///C:/Code/MyRepo/Customer.cs",
        "range": {
          "start": { "line": 2, "character": 13 },
          "end": { "line": 2, "character": 21 }
        }
      }
    }
  ]
}
```

当前默认能力只将 `workspaceSymbolProvider` 声明为 `true`，没有声明 `resolveProvider`；通用客户端不应假定 `workspaceSymbol/resolve` 可用。

## 重命名

服务器声明：

```json
{
  "renameProvider": {
    "prepareProvider": true
  }
}
```

推荐先调用 `textDocument/prepareRename`，确认当前位置可重命名并取得准确范围，再调用 `textDocument/rename`。

### textDocument/prepareRename

```json
{
  "jsonrpc": "2.0",
  "id": 100,
  "method": "textDocument/prepareRename",
  "params": {
    "textDocument": {
      "uri": "file:///C:/Code/MyRepo/Customer.cs"
    },
    "position": {
      "line": 2,
      "character": 15
    }
  }
}
```

当前 Roslyn 返回可重命名标识符的 `Range`：

```json
{
  "jsonrpc": "2.0",
  "id": 100,
  "result": {
    "start": { "line": 2, "character": 13 },
    "end": { "line": 2, "character": 21 }
  }
}
```

LSP 3.17 还允许返回带 `placeholder` 的对象或 `{ "defaultBehavior": true }`，但当前 Roslyn 不使用这两个分支。无法重命名时返回 `null`。

### textDocument/rename

```json
{
  "jsonrpc": "2.0",
  "id": 101,
  "method": "textDocument/rename",
  "params": {
    "textDocument": {
      "uri": "file:///C:/Code/MyRepo/Customer.cs"
    },
    "position": {
      "line": 2,
      "character": 15
    },
    "newName": "Client"
  }
}
```

输出是 `WorkspaceEdit`。简单客户端可处理 `changes` 映射：

```json
{
  "jsonrpc": "2.0",
  "id": 101,
  "result": {
    "changes": {
      "file:///C:/Code/MyRepo/Customer.cs": [
        {
          "range": {
            "start": { "line": 2, "character": 13 },
            "end": { "line": 2, "character": 21 }
          },
          "newText": "Client"
        }
      ],
      "file:///C:/Code/MyRepo/Program.cs": [
        {
          "range": {
            "start": { "line": 14, "character": 16 },
            "end": { "line": 14, "character": 24 }
          },
          "newText": "Client"
        }
      ]
    }
  }
}
```

根据客户端的 `workspaceEdit` 能力，结果也可能使用 `documentChanges`、带文档版本的 `TextDocumentEdit` 或文件操作。客户端应以 `initialize` 能力协商结果为准，并按工作区编辑的顺序原子或受控地应用修改。

## 代码操作与重构

服务器声明 `quickfix` 和 `refactor` 两类代码操作，并支持 `codeAction/resolve`。

### textDocument/codeAction

输入包括目标范围和上下文诊断：

```json
{
  "jsonrpc": "2.0",
  "id": 110,
  "method": "textDocument/codeAction",
  "params": {
    "textDocument": {
      "uri": "file:///C:/Code/MyRepo/Program.cs"
    },
    "range": {
      "start": { "line": 6, "character": 8 },
      "end": { "line": 6, "character": 20 }
    },
    "context": {
      "diagnostics": [
        {
          "range": {
            "start": { "line": 6, "character": 8 },
            "end": { "line": 6, "character": 20 }
          },
          "severity": 2,
          "code": "CS0168",
          "source": "csharp",
          "message": "变量已声明但从未使用"
        }
      ],
      "only": ["quickfix"]
    }
  }
}
```

输出是 `CodeAction` 与 `Command` 的联合数组。支持字面量代码操作的客户端通常得到：

```json
{
  "jsonrpc": "2.0",
  "id": 110,
  "result": [
    {
      "title": "删除未使用的变量",
      "kind": "quickfix",
      "diagnostics": [
        {
          "range": {
            "start": { "line": 6, "character": 8 },
            "end": { "line": 6, "character": 20 }
          },
          "code": "CS0168",
          "source": "csharp",
          "message": "变量已声明但从未使用"
        }
      ],
      "data": "<服务器返回的不透明数据>"
    }
  ]
}
```

`context.only` 用于限制类别。省略时可同时返回 Quick Fix 和重构。客户端必须保留 `data`。

### codeAction/resolve

把未解析的 `CodeAction` 原样发回。下例的占位字符串不可直接发送，客户端必须复用初始响应中的完整对象：

```json
{
  "jsonrpc": "2.0",
  "id": 111,
  "method": "codeAction/resolve",
  "params": {
    "title": "删除未使用的变量",
    "kind": "quickfix",
    "data": "<服务器返回的不透明数据>"
  }
}
```

响应可能补充 `edit` 或 `command`：

```json
{
  "jsonrpc": "2.0",
  "id": 111,
  "result": {
    "title": "删除未使用的变量",
    "kind": "quickfix",
    "edit": {
      "changes": {
        "file:///C:/Code/MyRepo/Program.cs": [
          {
            "range": {
              "start": { "line": 6, "character": 8 },
              "end": { "line": 6, "character": 21 }
            },
            "newText": ""
          }
        ]
      }
    },
    "data": "<服务器返回的不透明数据>"
  }
}
```

客户端通常先应用 `edit`，再在存在 `command` 时执行命令。

## 格式化

格式化结果都是 `TextEdit[]` 或 `null`。客户端应按原文档坐标应用编辑；若返回多个编辑，不应在每应用一项后用新坐标解释下一项。

### textDocument/formatting

```json
{
  "jsonrpc": "2.0",
  "id": 120,
  "method": "textDocument/formatting",
  "params": {
    "textDocument": {
      "uri": "file:///C:/Code/MyRepo/Program.cs"
    },
    "options": {
      "tabSize": 4,
      "insertSpaces": true,
      "trimTrailingWhitespace": true,
      "insertFinalNewline": true
    }
  }
}
```

```json
{
  "jsonrpc": "2.0",
  "id": 120,
  "result": [
    {
      "range": {
        "start": { "line": 0, "character": 0 },
        "end": { "line": 12, "character": 0 }
      },
      "newText": "using System;\n\nConsole.WriteLine(\"Hello\");\n"
    }
  ]
}
```

### textDocument/rangeFormatting

在全文格式化参数基础上增加 `range`：

```json
{
  "jsonrpc": "2.0",
  "id": 121,
  "method": "textDocument/rangeFormatting",
  "params": {
    "textDocument": {
      "uri": "file:///C:/Code/MyRepo/Program.cs"
    },
    "range": {
      "start": { "line": 4, "character": 0 },
      "end": { "line": 8, "character": 1 }
    },
    "options": {
      "tabSize": 4,
      "insertSpaces": true
    }
  }
}
```

响应仍为 `TextEdit[]`。

### textDocument/onTypeFormatting

服务器声明第一个触发字符为 `}`，其他触发字符包括 `;` 和换行。输入的 `position` 是触发字符输入后的光标位置，`ch` 是触发字符：

```json
{
  "jsonrpc": "2.0",
  "id": 122,
  "method": "textDocument/onTypeFormatting",
  "params": {
    "textDocument": {
      "uri": "file:///C:/Code/MyRepo/Program.cs"
    },
    "position": {
      "line": 8,
      "character": 1
    },
    "ch": "}",
    "options": {
      "tabSize": 4,
      "insertSpaces": true
    }
  }
}
```

客户端只应对服务器在能力中声明的字符发送此请求。

## 折叠范围

### textDocument/foldingRange

```json
{
  "jsonrpc": "2.0",
  "id": 130,
  "method": "textDocument/foldingRange",
  "params": {
    "textDocument": {
      "uri": "file:///C:/Code/MyRepo/Customer.cs"
    }
  }
}
```

```json
{
  "jsonrpc": "2.0",
  "id": 130,
  "result": [
    {
      "startLine": 2,
      "startCharacter": 0,
      "endLine": 12,
      "endCharacter": 1,
      "kind": "region"
    },
    {
      "startLine": 0,
      "endLine": 0,
      "kind": "imports"
    }
  ]
}
```

客户端能力会影响服务器是否可使用字符级折叠、折叠种类以及折叠范围数量限制。

## 选择范围

### textDocument/selectionRange

一个请求可以包含多个位置：

```json
{
  "jsonrpc": "2.0",
  "id": 140,
  "method": "textDocument/selectionRange",
  "params": {
    "textDocument": {
      "uri": "file:///C:/Code/MyRepo/Program.cs"
    },
    "positions": [
      { "line": 6, "character": 18 }
    ]
  }
}
```

响应数组与输入位置一一对应。每一项通过 `parent` 形成从小到大的嵌套范围：

```json
{
  "jsonrpc": "2.0",
  "id": 140,
  "result": [
    {
      "range": {
        "start": { "line": 6, "character": 16 },
        "end": { "line": 6, "character": 24 }
      },
      "parent": {
        "range": {
          "start": { "line": 6, "character": 8 },
          "end": { "line": 6, "character": 26 }
        },
        "parent": {
          "range": {
            "start": { "line": 5, "character": 4 },
            "end": { "line": 7, "character": 5 }
          }
        }
      }
    }
  ]
}
```

## 调用层次结构

调用层次结构分为准备和展开两阶段。客户端必须保留准备阶段返回项中的 `data`。

### textDocument/prepareCallHierarchy

```json
{
  "jsonrpc": "2.0",
  "id": 150,
  "method": "textDocument/prepareCallHierarchy",
  "params": {
    "textDocument": {
      "uri": "file:///C:/Code/MyRepo/CustomerService.cs"
    },
    "position": {
      "line": 8,
      "character": 17
    }
  }
}
```

```json
{
  "jsonrpc": "2.0",
  "id": 150,
  "result": [
    {
      "name": "GetCustomer",
      "kind": 6,
      "detail": "CustomerService",
      "uri": "file:///C:/Code/MyRepo/CustomerService.cs",
      "range": {
        "start": { "line": 8, "character": 4 },
        "end": { "line": 12, "character": 5 }
      },
      "selectionRange": {
        "start": { "line": 8, "character": 11 },
        "end": { "line": 8, "character": 22 }
      },
      "data": "<服务器返回的不透明数据>"
    }
  ]
}
```

### callHierarchy/incomingCalls

输入是准备阶段返回的完整 `item`。下例的占位字符串不可直接发送，必须使用准备阶段返回的真实 `data`：

```json
{
  "jsonrpc": "2.0",
  "id": 151,
  "method": "callHierarchy/incomingCalls",
  "params": {
    "item": {
      "name": "GetCustomer",
      "kind": 6,
      "uri": "file:///C:/Code/MyRepo/CustomerService.cs",
      "range": {
        "start": { "line": 8, "character": 4 },
        "end": { "line": 12, "character": 5 }
      },
      "selectionRange": {
        "start": { "line": 8, "character": 11 },
        "end": { "line": 8, "character": 22 }
      },
      "data": "<服务器返回的不透明数据>"
    }
  }
}
```

输出中的 `from` 是调用方，`fromRanges` 是调用方文档中的调用位置：

```json
{
  "jsonrpc": "2.0",
  "id": 151,
  "result": [
    {
      "from": {
        "name": "Main",
        "kind": 6,
        "uri": "file:///C:/Code/MyRepo/Program.cs",
        "range": {
          "start": { "line": 3, "character": 0 },
          "end": { "line": 15, "character": 1 }
        },
        "selectionRange": {
          "start": { "line": 3, "character": 12 },
          "end": { "line": 3, "character": 16 }
        },
        "data": "<服务器返回的不透明数据>"
      },
      "fromRanges": [
        {
          "start": { "line": 10, "character": 18 },
          "end": { "line": 10, "character": 29 }
        }
      ]
    }
  ]
}
```

### callHierarchy/outgoingCalls

输入同样是完整 `item`。输出中的 `to` 是被调用方，`fromRanges` 位于输入项代表的调用方文档中：

```json
{
  "jsonrpc": "2.0",
  "id": 152,
  "method": "callHierarchy/outgoingCalls",
  "params": {
    "item": {
      "name": "Main",
      "kind": 6,
      "uri": "file:///C:/Code/MyRepo/Program.cs",
      "range": {
        "start": { "line": 3, "character": 0 },
        "end": { "line": 15, "character": 1 }
      },
      "selectionRange": {
        "start": { "line": 3, "character": 12 },
        "end": { "line": 3, "character": 16 }
      },
      "data": "<服务器返回的不透明数据>"
    }
  }
}
```

没有调用关系时返回 `null` 或空数组。

## 类型层次结构

类型层次结构同样分为准备和展开两阶段。

### textDocument/prepareTypeHierarchy

```json
{
  "jsonrpc": "2.0",
  "id": 160,
  "method": "textDocument/prepareTypeHierarchy",
  "params": {
    "textDocument": {
      "uri": "file:///C:/Code/MyRepo/Customer.cs"
    },
    "position": {
      "line": 2,
      "character": 15
    }
  }
}
```

```json
{
  "jsonrpc": "2.0",
  "id": 160,
  "result": [
    {
      "name": "Customer",
      "kind": 5,
      "detail": "MyRepo.Models",
      "uri": "file:///C:/Code/MyRepo/Customer.cs",
      "range": {
        "start": { "line": 2, "character": 0 },
        "end": { "line": 12, "character": 1 }
      },
      "selectionRange": {
        "start": { "line": 2, "character": 13 },
        "end": { "line": 2, "character": 21 }
      },
      "data": "<服务器返回的不透明数据>"
    }
  ]
}
```

### typeHierarchy/supertypes

以下展开请求中的占位字符串不可直接发送，必须使用准备阶段返回的真实 `data`。

```json
{
  "jsonrpc": "2.0",
  "id": 161,
  "method": "typeHierarchy/supertypes",
  "params": {
    "item": {
      "name": "Customer",
      "kind": 5,
      "uri": "file:///C:/Code/MyRepo/Customer.cs",
      "range": {
        "start": { "line": 2, "character": 0 },
        "end": { "line": 12, "character": 1 }
      },
      "selectionRange": {
        "start": { "line": 2, "character": 13 },
        "end": { "line": 2, "character": 21 }
      },
      "data": "<服务器返回的不透明数据>"
    }
  }
}
```

输出为直接基类和接口对应的 `TypeHierarchyItem[]`。

### typeHierarchy/subtypes

方法和输入项形状相同：

```json
{
  "jsonrpc": "2.0",
  "id": 162,
  "method": "typeHierarchy/subtypes",
  "params": {
    "item": {
      "name": "Customer",
      "kind": 5,
      "uri": "file:///C:/Code/MyRepo/Customer.cs",
      "range": {
        "start": { "line": 2, "character": 0 },
        "end": { "line": 12, "character": 1 }
      },
      "selectionRange": {
        "start": { "line": 2, "character": 13 },
        "end": { "line": 2, "character": 21 }
      },
      "data": "<服务器返回的不透明数据>"
    }
  }
}
```

输出为直接派生类型或实现类型。客户端应继续使用返回项中的 `data` 展开后续层级。

## 语义标记

服务器始终声明范围语义标记：

```json
{
  "semanticTokensProvider": {
    "legend": {
      "tokenTypes": ["namespace", "class", "method"],
      "tokenModifiers": ["static", "declaration"]
    },
    "range": true,
    "full": false
  }
}
```

实际 `legend` 由服务器初始化响应提供。客户端必须按该数组索引解释结果，不能硬编码 Roslyn 的类型顺序。

如果客户端明确支持 range，服务器优先只提供 range；客户端不支持 range 时，服务器提供 full。当前默认能力不声明 full delta。

### textDocument/semanticTokens/range

```json
{
  "jsonrpc": "2.0",
  "id": 170,
  "method": "textDocument/semanticTokens/range",
  "params": {
    "textDocument": {
      "uri": "file:///C:/Code/MyRepo/Program.cs"
    },
    "range": {
      "start": { "line": 0, "character": 0 },
      "end": { "line": 30, "character": 0 }
    }
  }
}
```

```json
{
  "jsonrpc": "2.0",
  "id": 170,
  "result": {
    "data": [
      0, 6, 6, 0, 0,
      2, 13, 7, 1, 1
    ]
  }
}
```

`data` 每五个整数表示一个标记：

1. `deltaLine`：相对前一个标记的行增量。
2. `deltaStart`：同一行时相对前一个标记起点的字符增量；换行后相对行首。
3. `length`：UTF-16 代码单元长度。
4. `tokenType`：`legend.tokenTypes` 的索引。
5. `tokenModifiers`：位集合，第 N 位对应 `legend.tokenModifiers[N]`。

上例第一项表示第 0 行第 6 字符开始、长度 6、类型索引 0、无修饰符；第二项位于其后第 2 行第 13 字符，类型索引 1，修饰符位集合为 1。

### textDocument/semanticTokens/full

仅当初始化响应的 `semanticTokensProvider.full` 为 `true` 时调用：

```json
{
  "jsonrpc": "2.0",
  "id": 171,
  "method": "textDocument/semanticTokens/full",
  "params": {
    "textDocument": {
      "uri": "file:///C:/Code/MyRepo/Program.cs"
    }
  }
}
```

输出格式与 range 相同。客户端不应调用未被服务器声明的 `/full` 或 `/full/delta` 方法。

## CodeLens

服务器声明 `resolveProvider: true`。初始请求可以返回尚未包含 `command` 的 CodeLens，客户端随后解析可见项。

### textDocument/codeLens

```json
{
  "jsonrpc": "2.0",
  "id": 180,
  "method": "textDocument/codeLens",
  "params": {
    "textDocument": {
      "uri": "file:///C:/Code/MyRepo/Customer.cs"
    }
  }
}
```

```json
{
  "jsonrpc": "2.0",
  "id": 180,
  "result": [
    {
      "range": {
        "start": { "line": 2, "character": 0 },
        "end": { "line": 2, "character": 21 }
      },
      "data": "<服务器返回的不透明数据>"
    }
  ]
}
```

### codeLens/resolve

下例的占位字符串不可直接发送；`params` 必须是初始响应中的完整 CodeLens 对象。

```json
{
  "jsonrpc": "2.0",
  "id": 181,
  "method": "codeLens/resolve",
  "params": {
    "range": {
      "start": { "line": 2, "character": 0 },
      "end": { "line": 2, "character": 21 }
    },
    "data": "<服务器返回的不透明数据>"
  }
}
```

响应可能补充可执行命令：

```json
{
  "jsonrpc": "2.0",
  "id": 181,
  "result": {
    "range": {
      "start": { "line": 2, "character": 0 },
      "end": { "line": 2, "character": 21 }
    },
    "command": {
      "title": "3 个引用",
      "command": "roslyn.showReferences",
      "arguments": []
    },
    "data": "<服务器返回的不透明数据>"
  }
}
```

命令名称和参数属于服务器返回数据，客户端不应从示例推断固定命令集合；应按返回值和自身支持情况处理。

## Inlay Hint

服务器声明 `resolveProvider: true`。

### textDocument/inlayHint

请求必须指定可见范围：

```json
{
  "jsonrpc": "2.0",
  "id": 190,
  "method": "textDocument/inlayHint",
  "params": {
    "textDocument": {
      "uri": "file:///C:/Code/MyRepo/Program.cs"
    },
    "range": {
      "start": { "line": 0, "character": 0 },
      "end": { "line": 30, "character": 0 }
    }
  }
}
```

```json
{
  "jsonrpc": "2.0",
  "id": 190,
  "result": [
    {
      "position": { "line": 10, "character": 24 },
      "label": "value:",
      "kind": 2,
      "paddingRight": true,
      "data": "<服务器返回的不透明数据>"
    }
  ]
}
```

`label` 既可以是字符串，也可以是 `InlayHintLabelPart[]`。`kind` 的 `1` 表示类型提示，`2` 表示参数提示。

### inlayHint/resolve

下例的占位字符串不可直接发送；`params` 必须是初始响应中的完整 Inlay Hint 对象。

```json
{
  "jsonrpc": "2.0",
  "id": 191,
  "method": "inlayHint/resolve",
  "params": {
    "position": { "line": 10, "character": 24 },
    "label": "value:",
    "kind": 2,
    "data": "<服务器返回的不透明数据>"
  }
}
```

响应仍是 `InlayHint`。LSP 协议允许 resolve 补充 `tooltip`、`textEdits` 或标签部分的信息；当前 Roslyn resolve 实际补充 `tooltip`，而 `textEdits` 如有会在初始响应中给出。客户端应原样保留未知字段和 `data`。

## 文档同步

服务器声明增量同步：

```json
{
  "textDocumentSync": {
    "openClose": true,
    "change": 2,
    "save": {
      "includeText": false
    }
  }
}
```

其中 `change: 2` 表示 `Incremental`。以下方法都是通知，不包含 `id`，也没有响应。

### textDocument/didOpen

```json
{
  "jsonrpc": "2.0",
  "method": "textDocument/didOpen",
  "params": {
    "textDocument": {
      "uri": "file:///C:/Code/MyRepo/Program.cs",
      "languageId": "csharp",
      "version": 1,
      "text": "using System;\n\nConsole.WriteLine(\"Hello\");\n"
    }
  }
}
```

`didOpen` 必须携带全文。`languageId` 通常使用客户端注册的语言标识，例如 `csharp`、`vb` 或 `razor`。

### textDocument/didChange

增量变更包含旧版本文档中的范围和替换文本：

```json
{
  "jsonrpc": "2.0",
  "method": "textDocument/didChange",
  "params": {
    "textDocument": {
      "uri": "file:///C:/Code/MyRepo/Program.cs",
      "version": 2
    },
    "contentChanges": [
      {
        "range": {
          "start": { "line": 2, "character": 19 },
          "end": { "line": 2, "character": 24 }
        },
        "rangeLength": 5,
        "text": "World"
      }
    ]
  }
}
```

同一通知包含多项变化时，应按协议约定依次解释；最简单可靠的客户端通常按编辑器产生顺序发送非重叠增量。`version` 应严格递增。

### textDocument/didSave

服务器声明保存通知不需要全文：

```json
{
  "jsonrpc": "2.0",
  "method": "textDocument/didSave",
  "params": {
    "textDocument": {
      "uri": "file:///C:/Code/MyRepo/Program.cs"
    }
  }
}
```

### textDocument/didClose

```json
{
  "jsonrpc": "2.0",
  "method": "textDocument/didClose",
  "params": {
    "textDocument": {
      "uri": "file:///C:/Code/MyRepo/Program.cs"
    }
  }
}
```

关闭后，服务器不再把客户端内存中的未保存文本作为该文档的活动快照。

## 工作区文件重命名

只有客户端声明 `workspace.fileOperations`，且服务器组合中存在文件重命名 listener 时，服务器才会在 `workspace.fileOperations.willRename` 中返回注册过滤器。客户端应读取这些 glob 过滤器，只为匹配的文件发送请求。当前产品代码中的 listener 面向 Razor 文件；不要假定独立服务器会为普通 `.cs` 或 `.vb` 文件注册此能力。

### workspace/willRenameFiles

这是请求，不是通知；客户端应在实际重命名文件之前等待结果并应用返回的工作区编辑。

```json
{
  "jsonrpc": "2.0",
  "id": 200,
  "method": "workspace/willRenameFiles",
  "params": {
    "files": [
      {
        "oldUri": "file:///C:/Code/MyRepo/Pages/Customer.razor",
        "newUri": "file:///C:/Code/MyRepo/Pages/Client.razor"
      }
    ]
  }
}
```

匹配的 listener 可能返回配套的 `WorkspaceEdit`。下面只展示协议形状，不表示当前 Razor listener 必然产生该项编辑：

```json
{
  "jsonrpc": "2.0",
  "id": 200,
  "result": {
    "changes": {
      "file:///C:/Code/MyRepo/Pages/Customer.razor": [
        {
          "range": {
            "start": { "line": 0, "character": 9 },
            "end": { "line": 0, "character": 17 }
          },
          "newText": "Client"
        }
      ]
    }
  }
}
```

不需要附加编辑时返回 `null`。文件系统重命名本身仍由客户端执行；`WorkspaceEdit` 只表示服务器要求配套应用的工作区修改。

## Pull diagnostics

Roslyn 使用 LSP 3.17 pull diagnostics。客户端应优先按服务器初始化结果或动态注册决定是否发送诊断请求，不应仅凭方法名存在就调用。

### 能力注册方式

如果客户端明确设置：

```json
{
  "textDocument": {
    "diagnostic": {
      "dynamicRegistration": false
    }
  }
}
```

服务器在初始化结果中静态声明 `diagnosticProvider`，并设置 `interFileDependencies: true`。

如果 `dynamicRegistration` 为 `true`，服务器在收到 `initialized` 后通过 `client/registerCapability` 注册一个或多个诊断源。注册项的方法为 `textDocument/diagnostic`，并通过 `identifier` 区分来源；部分注册还会设置 `workspaceDiagnostics: true`。

客户端必须实现 `client/registerCapability` 才能使用动态注册分支。

### textDocument/diagnostic

输入的主要字段：

- `textDocument.uri`：目标文档。
- `identifier`：动态注册提供的诊断源名称；使用动态注册时应传回。
- `previousResultId`：上次报告的结果 ID，用于请求 unchanged 报告。
- `workDoneToken`、`partialResultToken`：可选进度令牌。

```json
{
  "jsonrpc": "2.0",
  "id": 210,
  "method": "textDocument/diagnostic",
  "params": {
    "textDocument": {
      "uri": "file:///C:/Code/MyRepo/Program.cs"
    },
    "identifier": "document",
    "previousResultId": null
  }
}
```

完整报告使用 `kind: "full"`：

```json
{
  "jsonrpc": "2.0",
  "id": 210,
  "result": {
    "kind": "full",
    "resultId": "42",
    "items": [
      {
        "range": {
          "start": { "line": 6, "character": 8 },
          "end": { "line": 6, "character": 20 }
        },
        "severity": 2,
        "code": "CS0168",
        "source": "csharp",
        "message": "变量已声明但从未使用"
      }
    ]
  }
}
```

后续请求传入 `previousResultId: "42"`，若结果未变化，服务器可以返回：

```json
{
  "jsonrpc": "2.0",
  "id": 211,
  "result": {
    "kind": "unchanged",
    "resultId": "42"
  }
}
```

诊断还可能包含：

- `severity`：`1` Error、`2` Warning、`3` Information、`4` Hint。
- `code`：字符串或数字诊断代码。
- `codeDescription.href`：诊断说明链接。
- `tags`：例如不必要代码或已弃用代码。
- `relatedInformation`：其他文档位置的相关信息。
- `data`：服务器保留数据。

### workspace/diagnostic

工作区诊断可能耗时较长，客户端应支持 work-done progress 和 partial result。

```json
{
  "jsonrpc": "2.0",
  "id": 220,
  "method": "workspace/diagnostic",
  "params": {
    "identifier": "workspace",
    "previousResultIds": [
      {
        "uri": "file:///C:/Code/MyRepo/Program.cs",
        "value": "42"
      }
    ],
    "workDoneToken": "workspace-diagnostics-1",
    "partialResultToken": "workspace-diagnostics-partial-1"
  }
}
```

当前 Roslyn 工作区诊断返回未打开文档的完整报告，`version` 为 `null`。最终结果可以包含多个文档报告：

```json
{
  "jsonrpc": "2.0",
  "id": 220,
  "result": {
    "items": [
      {
        "uri": "file:///C:/Code/MyRepo/Program.cs",
        "version": null,
        "kind": "full",
        "resultId": "42",
        "items": []
      },
      {
        "uri": "file:///C:/Code/MyRepo/Customer.cs",
        "version": null,
        "kind": "full",
        "resultId": "43",
        "items": []
      }
    ]
  }
}
```

服务器也可以通过 `$/progress` 发送 `WorkspaceDiagnosticReportPartialResult`。客户端应按 URI 合并报告；同一 URI 多次出现时，以最后一次报告为准。LSP 协议允许工作区报告使用 `unchanged` 分支，但当前 Roslyn 工作区处理器跳过未变化的报告，不生成该分支。

## 客户端能力协商建议

为了获得较完整且可预测的标准能力，客户端可以在 `initialize.params.capabilities` 中声明：

```json
{
  "textDocument": {
    "hover": {
      "contentFormat": ["markdown", "plaintext"]
    },
    "definition": {
      "linkSupport": true
    },
    "typeDefinition": {
      "linkSupport": true
    },
    "implementation": {
      "linkSupport": true
    },
    "documentSymbol": {
      "hierarchicalDocumentSymbolSupport": true
    },
    "codeAction": {
      "codeActionLiteralSupport": {
        "codeActionKind": {
          "valueSet": ["quickfix", "refactor"]
        }
      },
      "resolveSupport": {
        "properties": ["edit"]
      }
    },
    "completion": {
      "completionItem": {
        "documentationFormat": ["markdown", "plaintext"],
        "resolveSupport": {
          "properties": ["documentation", "detail", "additionalTextEdits"]
        }
      }
    },
    "semanticTokens": {
      "requests": {
        "range": true
      },
      "tokenTypes": [],
      "tokenModifiers": [],
      "formats": ["relative"]
    },
    "diagnostic": {
      "dynamicRegistration": true
    },
    "inlayHint": {
      "resolveSupport": {
        "properties": ["tooltip"]
      }
    }
  },
  "workspace": {
    "workspaceFolders": true,
    "applyEdit": true,
    "workspaceEdit": {
      "documentChanges": true,
      "resourceOperations": ["create", "rename", "delete"]
    },
    "fileOperations": {
      "willRename": true
    }
  },
  "window": {
    "workDoneProgress": true
  }
}
```

上例是能力结构示意，不是所有客户端都必须逐项复制。尤其是语义标记的 `tokenTypes` 和 `tokenModifiers`，正式客户端应填写自身理解的标准类型集合。

## 错误、取消和进度

### JSON-RPC 错误

请求失败时，服务器返回 `error` 而不是 `result`：

```json
{
  "jsonrpc": "2.0",
  "id": 100,
  "error": {
    "code": -32602,
    "message": "Invalid params"
  }
}
```

客户端应至少处理标准 JSON-RPC/LSP 错误码、服务器异常和内容已修改等情况。

### 取消请求

长时间请求可通过通知取消：

```json
{
  "jsonrpc": "2.0",
  "method": "$/cancelRequest",
  "params": {
    "id": 60
  }
}
```

取消是尽力而为；客户端仍应接受请求恰好在取消前完成的正常响应。

### 进度

带 `workDoneToken` 或 `partialResultToken` 的请求可能收到：

```json
{
  "jsonrpc": "2.0",
  "method": "$/progress",
  "params": {
    "token": "references-1",
    "value": {
      "kind": "report",
      "message": "正在查找引用"
    }
  }
}
```

客户端必须按 token 将进度关联到原请求，并区分工作进度与部分结果。

## 实现位置

- 默认能力：`src/LanguageServer/Protocol/DefaultCapabilitiesProvider.cs`
- 标准方法定义：`src/LanguageServer/Protocol/Protocol/Methods.*.cs`
- 请求和通知处理器：`src/LanguageServer/Protocol/Handler/`
- 协议数据类型：`src/LanguageServer/Protocol/Protocol/`
- 动态诊断注册：`src/LanguageServer/Protocol/Handler/Diagnostics/Public/PublicDocumentPullDiagnosticsHandler_IOnInitialized.cs`

## 相关资料

- [roslyn-language-server 使用指南](roslyn-language-server-usage.zh-cn.md)
- [Language Server Protocol 3.17](https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/)
- [项目自带的 roslyn-language-server README](../src/LanguageServer/Microsoft.CodeAnalysis.LanguageServer/README.md)
