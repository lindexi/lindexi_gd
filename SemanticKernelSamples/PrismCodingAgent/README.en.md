# PrismCodingAgent

English | [简体中文](README.md)

An AI coding-agent desktop application built with Avalonia and .NET 10. Organize tasks around local code workspaces, chat with OpenAI-compatible model services, and use tools to read, analyze, and modify code while following execution in the UI.

![PrismCodingAgent English UI](Docs/Images/coding-chat-en.png)

## Features

- **Work tasks**: Create, switch, and rename tasks; select a workspace, model, and reasoning effort per task. Multiple tasks can run independently.
- **History and archives**: Browse and restore session history, archive tasks, and restore them from the archive list. Task information is restored on restart, but execution does not resume automatically.
- **Model services**: Configure OpenAI-compatible endpoints, API keys, and models across multiple providers. Chat and Responses API run options are available; capabilities depend on the service.
- **Tools and sub-agents**: Inspect tool calls and results, including file operations, Roslyn code queries, and .NET build/test tools. Chat mode supports coding sub-agent delegation; image analysis tools are also available.
- **UI and connection settings**: Simplified Chinese and English interfaces, optional network proxy settings, and an optional Windows sandbox tool connection.

## Usage

### First-time setup and starting a task

Start from source as described under “Development” below. On first launch:

1. Add a model service from the welcome screen or settings. Enter its **endpoint** (for example, `https://api.openai.com/v1`, subject to your provider's requirements), **API key**, and provider-supplied **model ID**. Choose a default model and save.
2. Create or select a work task. Enter a local project directory in the workspace field and apply it; select the task's model and adjust reasoning effort if needed.
3. Describe your goal in the input box. Click **Send** or press **Ctrl+Enter**; Enter inserts a new line. Inspect streaming replies, tool calls, and sub-agent details as the task runs.
4. Click **Stop** to cancel the current task's run. Switch tasks to inspect other work; stopping one task does not stop the others.
5. Restore conversations from session history. Archive inactive tasks through the task menu and restore them from the archive list when needed.

Tools can modify workspace files or execute builds and tests. Use trusted projects and review changes through version control. Models must support the relevant tool calls; Responses API and Chat capabilities are not identical.

### Language, settings, and local data

- On first launch, the application matches the system UI culture or its parent cultures to `zh-Hans` or `en`, falling back to Simplified Chinese when neither matches.
- Select a language in settings and **save** to apply it immediately and persist it. Subsequent launches use the saved language. Model, proxy, and sandbox changes also require saving; reopen the workspace after changing runtime environment settings.
- A network proxy is optional. Windows sandbox integration is disabled by default. Before enabling it, provide a working `WinRemoteShell.exe` and server address; settings include a connection test.
- Data is stored in the `CodingChatRoom` directory under the system's `LocalApplicationData`: `AgentConfiguration.json` holds model configuration, `ShellSettings.json` holds UI and connection settings, and `WorkTasks.json` holds task information. There are also `Sessions`, `Logs`, and `Abilities` directories. Model configuration contains API keys; do not commit or publicly share this local data.

### UI demos

The launch profiles include `中文演示` (`--demo-zh`) and `英文演示` (`--demo-en`). Demos use built-in task, message, tool, and sub-agent presentation data. They **do not call real models or execute tools**, and cannot validate service connectivity or coding results.

## Development

### Prerequisites and layout

Install the **.NET 10 SDK**. Keep the full **SemanticKernelSamples parent workspace**, rather than copying only `PrismCodingAgent`: the application uses project references to the adjacent `AgentLib/AgentLib.Coding` project and its dependencies, not a standalone NuGet dependency.

```text
SemanticKernelSamples/
├─ AgentLib/                         # Shared agents and coding tools
└─ PrismCodingAgent/
   ├─ PrismCodingAgent.slnx          # Product solution
   ├─ Code/PrismCodingAgent.AvaloniaShell/
   ├─ Tests/PrismCodingAgent.AvaloniaShell.Tests/
   └─ Docs/                         # Designs, investigations, and images
```

Semantic code tools use `roslyn-language-server`; its launch command must be available. Build/test tools also require the appropriate SDK and local environment. An Avalonia desktop UI does not imply that every tool supports every platform. Windows sandbox features require the corresponding Windows environment and external service.

### Build, run, and test

**Run all commands below from the `SemanticKernelSamples` root**, not from the `PrismCodingAgent` directory.

```sh
dotnet build PrismCodingAgent/PrismCodingAgent.slnx

dotnet run --project PrismCodingAgent/Code/PrismCodingAgent.AvaloniaShell/PrismCodingAgent.AvaloniaShell.csproj --launch-profile PrismCodingAgent.AvaloniaShell

dotnet test PrismCodingAgent/Tests/PrismCodingAgent.AvaloniaShell.Tests/PrismCodingAgent.AvaloniaShell.Tests.csproj --filter "TestCategory!=LiveApi"
```

Regular tests use MSTest and Avalonia.Headless. The filter excludes `LiveApi` tests that access real model services. Prepare valid configuration separately for live-service tests; they may incur API charges.

Run the Chinese or English demo through its profile:

```sh
dotnet run --project PrismCodingAgent/Code/PrismCodingAgent.AvaloniaShell/PrismCodingAgent.AvaloniaShell.csproj --launch-profile "中文演示"
dotnet run --project PrismCodingAgent/Code/PrismCodingAgent.AvaloniaShell/PrismCodingAgent.AvaloniaShell.csproj --launch-profile "英文演示"
```

Alternatively, bypass launch profiles and pass the argument directly (replace `--demo-en` with `--demo-zh` for Chinese):

```sh
dotnet run --project PrismCodingAgent/Code/PrismCodingAgent.AvaloniaShell/PrismCodingAgent.AvaloniaShell.csproj --no-launch-profile -- --demo-en
```

The `调试欢迎界面` profile maps to `--debug-onboarding` and uses a separate temporary data directory for debugging first-time setup.

### Code entry points

Application-relative paths below are under `Code/PrismCodingAgent.AvaloniaShell/`:

| Entry point | Responsibility |
| --- | --- |
| [Program.cs](Code/PrismCodingAgent.AvaloniaShell/Program.cs) | Avalonia desktop startup and platform detection |
| [App.axaml.cs](Code/PrismCodingAgent.AvaloniaShell/App.axaml.cs) | Startup options, settings, tasks, and main-window initialization; demo mode selection |
| [ViewModels/MainViewModel.cs](Code/PrismCodingAgent.AvaloniaShell/ViewModels/MainViewModel.cs) | Work tasks, archives, history access, and global settings coordination |
| [ViewModels/ChatViewModel.cs](Code/PrismCodingAgent.AvaloniaShell/ViewModels/ChatViewModel.cs) | Input, model selection, workspace, send, and stop interactions |
| [Services/WorkTasks/CodingWorkTaskRuntimeFactory.cs](Code/PrismCodingAgent.AvaloniaShell/Services/WorkTasks/CodingWorkTaskRuntimeFactory.cs) | Per-task model, session store, controller, and coding-agent assembly |
| [Services/WorkTasks/CodingWorkTaskRuntime.cs](Code/PrismCodingAgent.AvaloniaShell/Services/WorkTasks/CodingWorkTaskRuntime.cs) | Task runtime resource ownership |
| [Services/Chat](Code/PrismCodingAgent.AvaloniaShell/Services/Chat) | Chat / Responses execution adapters |
| [AgentLib.Coding](../AgentLib/AgentLib.Coding) (parent workspace) | `CodingAgent.cs` execution entry point, `CodingWorkspaceCache.cs` tool assembly, `CodingSubAgentTools.cs` coding delegation, and tools such as `RoslynAgentTools.cs` / `DotNetCliTools.cs` |

See [Docs](Docs) for design and investigation material. It includes historical records and unimplemented proposals; use the source code as the authority for current behavior and paths.
