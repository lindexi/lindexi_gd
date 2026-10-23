# WorkspaceToolProvider 工具参数错误自然语言返回方案

## 1. 背景与目标

`WorkspaceToolProvider` 向 Agent 提供以下工作区工具：

- `ListDirectory`
- `FindEntriesByName`
- `FindFilesMatchingPattern`
- `ReadFileLines`
- `WriteFileContent`
- `ReplaceStringInFile`
- `MultiReplaceStringInFile`

目前，文件不存在、目录越界、正则表达式无效、写入前未读取等可恢复问题已经通过自然语言字符串返回给 Agent。但**部分工具参数错误仍会抛出异常**。工具执行框架向 Agent 展示的只有 `Fail` 状态，不含异常细节，Agent 因此无法判断出错原因，也就无从修正参数后重试。

本方案的目标是：把工具方法内部**可预期的参数错误**改为自然语言结果返回，让 Agent 能够理解错误、调整参数并重新发起调用。

## 2. 设计原则

### 2.1 保持主流程的类型语义

工具定义中本就要求必填、非空的参数（如 `filePath`、`query`、`content`、`oldString`、`newString`、`replacements`），**不得**仅为了防御模型显式传入 `null` 而改成 `string?` 等可空类型。可空标注是 API 契约的一部分，随意放宽会误导调用方，并削弱主流程的类型约束。

本次改造只处理两种情况：能正常进入工具方法、且由方法内参数校验发现的问题。

### 2.2 不做额外的调用边界防御

不考虑 `AIFunctionFactory` 在调用目标方法之前发生参数绑定失败的情况，也不新增 `AIFunction` 装饰器、调用过滤器或统一异常包装层。改造范围严格限定为工具方法内部当前主动抛出的参数异常，以及与这些参数直接相关、明确可恢复的问题。

### 2.3 参数错误以工具结果形式返回

凡 Agent 可以通过修正参数来恢复的错误，统一返回简洁、明确、可操作的自然语言文本，并统一加 `参数错误：` 前缀，便于 Agent 快速识别为「改参数即可解决」的问题。示例：

- `参数错误：maxResults 必须大于 0，当前值为 0。`
- `参数错误：query 不能为空或仅包含空白字符。`
- `参数错误：startLine 必须大于等于 1，当前值为 0。`
- `参数错误：endLine 必须大于等于 startLine，当前范围为 10-5。`
- `参数错误：单次最多读取 400 行，当前请求读取 500 行。`

### 2.4 不吞掉非参数异常

不使用 `catch (Exception)` 把一切运行时异常转成字符串。以下异常不属于本次改造范围：

- `OperationCanceledException`
- 非预期的 I/O 错误
- 权限错误
- 程序状态错误
- 内部实现缺陷

本次只替换已有的**主动参数校验异常**，不通过宽泛捕获掩盖真实故障。

### 2.5 不动全局参数校验

不修改 `ArgumentHelper.ThrowIfNullOrWhiteSpace` 的行为。该辅助方法可能被普通 .NET API 使用——对非 Agent 工具 API 而言，参数不合法时抛出 `ArgumentException` 仍是合理语义。工具方法应在自身入口显式判断并返回自然语言错误，而不是改变全局辅助方法。

## 3. 现状：各工具的参数异常点

下表汇总当前已发现的参数异常位置、现状行为与改造后的目标行为：

| 工具 | 参数条件 | 当前行为 | 目标行为 |
|---|---|---|---|
| `ListDirectory` | `maxResults <= 0` | 抛出 `ArgumentOutOfRangeException` | 返回参数错误文本 |
| `FindEntriesByName` | `query` 为空白 | `ArgumentHelper` 抛出异常 | 返回参数错误文本 |
| `FindEntriesByName` | `includeFiles` 与 `includeDirectories` 都为 `false` | 抛出 `ArgumentException` | 返回参数错误文本 |
| `FindEntriesByName` | `maxResults <= 0` | 抛出 `ArgumentOutOfRangeException` | 返回参数错误文本 |
| `FindFilesMatchingPattern` | `query` 为空白 | `ArgumentHelper` 抛出异常 | 返回参数错误文本 |
| `FindFilesMatchingPattern` | `maxResults <= 0` | 抛出 `ArgumentOutOfRangeException` | 返回参数错误文本 |
| `ReadFileLines` | `filePath` 为空白 | `ArgumentHelper` 抛出异常 | 返回参数错误文本 |
| `ReadFileLines` | `startLine <= 0` | 抛出 `ArgumentOutOfRangeException` | 返回参数错误文本 |
| `ReadFileLines` | `endLine < startLine` | 抛出 `ArgumentOutOfRangeException` | 返回参数错误文本 |
| `ReadFileLines` | 读取范围超过 400 行 | 抛出 `ArgumentOutOfRangeException` | 返回参数错误文本 |
| `WriteFileContent` | `filePath` 为空白 | `ArgumentHelper` 抛出异常 | 返回参数错误文本 |
| `ReplaceStringInFile` | `filePath` 为空白 | `ArgumentHelper` 抛出异常 | 返回参数错误文本 |
| `ReplaceStringInFile` | `oldString` 或 `newString` 为 `null` | 抛出 `ArgumentNullException` | 保持非空 API 契约，不为边缘 null 输入改签名，本次不处理 |
| `MultiReplaceStringInFile` | `replacements` 为 `null` | 抛出 `ArgumentNullException` | 保持非空 API 契约，本次不处理 |

## 4. 各工具的具体改造

### 4.1 `ListDirectory`

把 `maxResults <= 0` 的异常改为直接返回错误文本（方法签名保持 `Task<string>` 不变，使用 `Task.FromResult` 返回）：

```text
参数错误：maxResults 必须大于 0，当前值为 {maxResults}。
```

### 4.2 `FindEntriesByName`

按以下顺序校验：

1. `query` 非空且不全是空白字符；
2. `includeFiles` 与 `includeDirectories` 至少一个为 `true`；
3. `maxResults` 大于 0。

对应的错误文本：

```text
参数错误：query 不能为空或仅包含空白字符。
```

```text
参数错误：includeFiles 和 includeDirectories 至少有一个必须为 true。
```

```text
参数错误：maxResults 必须大于 0，当前值为 {maxResults}。
```

同时移除对 `ArgumentHelper.ThrowIfNullOrWhiteSpace(query)` 的调用，但保留 `query` 的非空类型声明。

### 4.3 `FindFilesMatchingPattern`

校验顺序：

1. `query` 非空且不全是空白字符；
2. `maxResults` 大于 0。

错误文本与其他工具保持一致的风格。现有正则表达式错误处理**保持不变**，它已经是 Agent 可见的自然语言结果，无需纳入本次改造：

```text
正则表达式无效: {具体错误}
```

### 4.4 `ReadFileLines`

按以下顺序校验：

1. `filePath` 非空且不全是空白字符；
2. `startLine` 大于等于 1；
3. `endLine` 大于等于 `startLine`；
4. `endLine - startLine + 1` 不超过 `DefaultMaxRangeLines`。

对应的错误文本：

```text
参数错误：filePath 不能为空或仅包含空白字符。
```

```text
参数错误：startLine 必须大于等于 1，当前值为 {startLine}。
```

```text
参数错误：endLine 必须大于等于 startLine，当前范围为 {startLine}-{endLine}。
```

```text
参数错误：单次最多读取 {DefaultMaxRangeLines} 行，当前请求读取 {requestedLineCount} 行。
```

校验通过后，继续沿用现有的路径解析、文件快照记录与 `WorkspaceFileLineReader` 读取流程，不做其他改动。

### 4.5 `WriteFileContent`

将 `filePath` 的 `ArgumentHelper.ThrowIfNullOrWhiteSpace` 替换为显式返回：

```text
参数错误：filePath 不能为空或仅包含空白字符。
```

`content` 的非空类型保持不变，不因模型可能传入 `null` 而改变 API 契约。

### 4.6 `ReplaceStringInFile`

将 `filePath` 的空白校验改为自然语言返回：

```text
参数错误：filePath 不能为空或仅包含空白字符。
```

`oldString` 与 `newString` 继续保持非空参数类型。现有的 `ArgumentNullException.ThrowIfNull` 是对非空 API 契约的运行时保护，不因边缘模型输入而改为可空。

此外，新增 `oldString.Length == 0` 的校验：

```text
参数错误：oldString 不能为空字符串。
```

空字符串虽然不是 `null`，但无法表达有效的唯一替换目标，还可能使底层匹配计数逻辑无法推进。这属于正常进入方法后的明确参数错误，应以自然语言返回。`newString` 允许为空字符串——删除目标文本本就是合理的替换操作。

### 4.7 `MultiReplaceStringInFile`

`replacements` 保持非空类型与现有非空契约。现有的空列表处理保持不变：

```text
替换操作列表为空，未执行任何操作。
```

对列表中的每个 `ReplaceOperation`，在执行替换前检查其参数值：

- `FilePath` 非空且不全是空白字符；
- `OldString` 非空字符串。

单个操作参数错误**不应中断整批操作**，应将该操作记录为失败，并继续执行后续操作。例如：

```text
批量替换完成: 1 个成功, 1 个失败。

操作 1: note.txt
  状态: 失败
  消息: 参数错误：oldString 不能为空字符串。
```

`NewString` 允许为空字符串。

## 5. 明确不实施的内容

本次改造不包含以下内容：

1. 不将非空工具参数改为可空参数；
2. 不新增统一 `AIFunction` 包装器；
3. 不处理目标方法调用之前的参数绑定错误；
4. 不捕获所有异常并转换为字符串；
5. 不修改 `ArgumentHelper` 的全局抛异常行为；
6. 不改变文件系统 I/O、权限与取消异常的现有传播方式；
7. 不引入新的接口、抽象层或外部依赖。

## 6. 测试方案

在现有 `AgentLib.Tests/WorkspaceToolProviderTests.cs` 中补充测试，沿用项目现有的 MSTest 风格。

### 6.1 `ListDirectory`

- `maxResults` 为 0 时不抛异常；
- 返回结果包含 `参数错误：maxResults 必须大于 0`。

### 6.2 `FindEntriesByName`

- `query` 为空字符串时返回参数错误；
- `query` 仅为空白字符时返回参数错误；
- 两个 include 参数都为 `false` 时返回参数错误；
- `maxResults` 为 0 时返回参数错误。

### 6.3 `FindFilesMatchingPattern`

- `query` 为空字符串时返回参数错误；
- `query` 仅为空白字符时返回参数错误；
- `maxResults` 为负数时返回参数错误；
- 现有无效正则表达式的测试行为保持不变。

### 6.4 `ReadFileLines`

- 空白 `filePath` 返回参数错误；
- `startLine` 为 0 或负数时返回参数错误；
- `endLine` 小于 `startLine` 时返回当前范围信息；
- 请求超过 400 行时返回最大行数与实际请求行数；
- 所有参数错误均不读取文件、不记录文件快照。

### 6.5 `WriteFileContent`

- 空白 `filePath` 返回参数错误；
- 参数错误时不创建或修改文件。

### 6.6 `ReplaceStringInFile`

- 空白 `filePath` 返回参数错误；
- 空 `oldString` 返回参数错误且不会修改文件；
- 空 `newString` 仍可正常删除唯一匹配文本。

### 6.7 `MultiReplaceStringInFile`

- 空替换列表保持现有提示；
- 单个操作的空白路径被记录为失败；
- 单个操作的空 `OldString` 被记录为失败；
- 某个操作参数错误时，后续合法操作仍会执行；
- 汇总的成功数与失败数准确。

测试重点是验证公开工具方法返回自然语言结果，不增加调用前参数绑定场景的测试。

## 7. 实施步骤

1. 在 `WorkspaceToolProvider` 中统一参数错误文本的风格；
2. 替换 `ListDirectory`、`FindEntriesByName`、`FindFilesMatchingPattern`、`ReadFileLines` 中的主动参数异常；
3. 替换 `WriteFileContent` 与 `ReplaceStringInFile` 的空白路径异常；
4. 为单项与批量替换增加空 `oldString` 校验；
5. 保持所有工具入口的非空参数类型不变；
6. 在 `WorkspaceToolProviderTests` 中增加对应的直接调用测试；
7. 构建 `AgentLib` 的 `net6.0` 与 `net9.0` 目标；
8. 运行 `AgentLib.Tests`，确认现有文件工具行为无回归。

## 8. 验收标准

改造完成后应满足：

- 工具方法内部当前主动抛出、且可由 Agent 修正的参数错误，一律改为自然语言返回；
- 错误信息明确指出参数名、约束条件及有参考价值的当前值；
- 必填参数继续使用非空类型，API 语义未因边缘输入而弱化；
- 未增加调用前参数绑定防御或全局异常包装；
- 未使用宽泛异常捕获掩盖内部错误；
- 正常读取、查询、写入与替换流程保持不变；
- 新增测试通过，现有测试无回归。
