using System.ComponentModel;
using AgentLib.Model;
using AgentLib.Tools;
using Microsoft.Extensions.AI;

namespace AgentLib.Coding;

internal sealed class CodingSubAgentTools(
    CodingAgent agent,
    CodingRunContext parentContext,
    CodingRunWorkspaceContext workspaceContext,
    IReadOnlyList<string> prompts,
    CodingChatRunOptions options)
{
    internal ToolRegistration CreateRegistration() => new(
        AIFunctionFactory.Create(InvokeCodingSubAgent, new AIFunctionFactoryOptions
        {
            ConfigureParameterBinding = parameter => parameter.ParameterType == typeof(CopilotChatSubAgentItem)
                ? new AIFunctionFactoryOptions.ParameterBindingOptions
                {
                    ExcludeFromSchema = true,
                    BindParameter = (_, arguments) => arguments.Context?[CodingToolInvocation.OutputKey]
                        as CopilotChatSubAgentItem
                        ?? throw new InvalidOperationException("A sub-agent invocation requires an output target."),
                }
                : default,
        }), DisplayKind: ToolCallDisplayKind.SubAgent);

    [Description("执行编程任务并返回提交的结果。")]
    private async Task<string> InvokeCodingSubAgent(
        [Description("本次编程任务提示词。")] string prompt,
        CopilotChatSubAgentItem childOutput,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var submission = new GeneratedTextSubmissionTool();
        CodingRunWorkspaceContext childTools = workspaceContext.WithTool(new ToolRegistration(
            submission.CreateTool("ReturnOutputToParent", "提交最终结果文本。")));
        var context = new CodingRunContext(parentContext.ChatClient,
            CopilotChatMessage.CreateAssistant(string.Empty, isPresetInfo: false),
            mainThreadDispatcher: parentContext.MainThreadDispatcher, output: childOutput);
        IReadOnlyList<string> childPrompts = [.. prompts, "最终结果必须通过 `ReturnOutputToParent` 提交"];
        ICodingAgentRunResult run = await agent.RunAsync(context, [new TextContent(prompt)], childTools,
            childPrompts, options, cancellationToken).ConfigureAwait(false);
        await run.CompletionTask.ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        if (!submission.HasSubmittedText)
        {
            run = await agent.RunAsync(context, [new TextContent("要求调用 ReturnOutputToParent 方法")],
                childTools, childPrompts, options, cancellationToken).ConfigureAwait(false);
            await run.CompletionTask.ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
        }

        if (!submission.HasSubmittedText)
        {
            submission.SubmitText("子智能体没有返回任何内容");
        }

        return submission.SubmittedText ?? string.Empty;
    }
}
