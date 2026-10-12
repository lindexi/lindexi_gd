using AgentLib.Model;
using AgentLib.Tools;
using Microsoft.Extensions.AI;

namespace AgentLib.Coding;

internal static class CodingToolInvocation
{
    internal static readonly object OutputKey = new();

    internal static async ValueTask<object?> InvokeAsync(CodingRunContext context,
        ToolRegistrationRegistry registry, FunctionInvocationContext invocation,
        CancellationToken cancellationToken)
    {
        ToolCallPresentation? presentation = registry.CreatePresentation(invocation.CallContent);
        if (presentation?.DisplayKind != ToolCallDisplayKind.SubAgent)
        {
            return await invocation.Function.InvokeAsync(invocation.Arguments, cancellationToken).ConfigureAwait(false);
        }

        string callId = invocation.CallContent.CallId;
        if (string.IsNullOrWhiteSpace(callId))
        {
            throw new InvalidOperationException("A tool invocation requires a call ID.");
        }

        await context.InvokeOutputAsync(() =>
        {
            context.Output.ClearPlaceholder();
            invocation.Arguments.Context ??= new Dictionary<object, object?>();
            invocation.Arguments.Context[OutputKey] = context.Output.CreateSubAgentItem(
                invocation.CallContent.Name,
                System.Text.Json.JsonSerializer.Serialize(invocation.CallContent.Arguments), callId);
        }).ConfigureAwait(false);

        try
        {
            return await invocation.Function.InvokeAsync(invocation.Arguments, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            invocation.Arguments.Context.Remove(OutputKey);
        }
    }
}
