# Copilot Instructions

这是一个对 MiniMax 后台封装的项目

## 项目指南

- 代码应该有文档注释（`<summary>` 注释）
  - 应该根据 MiniMax 文档的内容，为 DTO 类型补充注释。如果文档里面有涉及到 `注` 的部分，请在文档注释的 `<remarks>` 里面补充
  - 为公开的类型和公开的成员添加注释
- 应该遵循 C# .NET 的类库设计规范
- 代码需要考虑 AOT 兼容，确保构建的时候不会提示 AOT 相关异常，且保持 `<IsAotCompatible>True</IsAotCompatible>` 必定存在设置
- 项目代码符合 C# .NET 编码规范

## 常错点

- 应该使用 `Path.Join` 进行路径拼接，而不是 `Path.Combine` 进行路径拼接
- 尽量采用 `Span` 等方式进行对象分配