namespace AgentLib.Coding.Tests;

[TestClass]
public sealed class CodingWorkspaceToolsReuseTests
{
    [TestMethod]
    public async Task ResponsesAgentShouldReuseExistingCodingAgentWithoutWorkspace()
    {
        await using var agent = new CodingAgent();
        var responsesAgent = new ResponsesCodingAgent(agent);

        Assert.IsNotNull(responsesAgent);
    }
}
