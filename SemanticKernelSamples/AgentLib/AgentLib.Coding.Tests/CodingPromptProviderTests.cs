namespace AgentLib.Coding.Tests;

[TestClass]
public sealed class CodingPromptProviderTests
{
    [TestMethod]
    public async Task BuildAsyncShouldPreserveDefaultPromptOrder()
    {
        IReadOnlyList<string> prompts = await CodingPromptProvider.BuildAsync(null, CancellationToken.None);

        CollectionAssert.AreEqual(new[]
        {
            CodingPromptProvider.SystemPrompt,
            CodingPromptProvider.CodePrompt,
            CodingPromptProvider.SandboxPrompt,
        }, prompts.ToArray());
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public async Task BuildAsyncShouldUseDefaultCodePromptForBlankPath(string path)
    {
        IReadOnlyList<string> prompts = await CodingPromptProvider.BuildAsync(path, CancellationToken.None);

        Assert.AreEqual(CodingPromptProvider.CodePrompt, prompts[1]);
    }

    [TestMethod]
    public async Task BuildAsyncShouldHonorCancellationWithoutInstructionsFile()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            CodingPromptProvider.BuildAsync(null, cancellation.Token));
    }

    [TestMethod]
    public async Task BuildAsyncShouldReportMissingInstructionsFile()
    {
        string path = Path.Combine(Path.GetTempPath(), $"coding-instructions-{Guid.NewGuid():N}.md");

        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            CodingPromptProvider.BuildAsync(path, CancellationToken.None));
    }

    [TestMethod]
    public async Task BuildAsyncShouldAppendInstructionsOnlyToCodePrompt()
    {
        string path = Path.Combine(Path.GetTempPath(), $"coding-instructions-{Guid.NewGuid():N}.md");
        const string instructions = "Use the existing project conventions.";
        await File.WriteAllTextAsync(path, instructions);
        Console.WriteLine(path);

        IReadOnlyList<string> prompts = await CodingPromptProvider.BuildAsync(path, CancellationToken.None);

        Assert.AreEqual($"{CodingPromptProvider.CodePrompt}{Environment.NewLine}{Environment.NewLine}```markdown {path}{Environment.NewLine}{instructions}{Environment.NewLine}```", prompts[1]);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow(" \r\n ")]
    public async Task BuildAsyncShouldUseDefaultCodePromptForBlankFile(string instructions)
    {
        string path = Path.Combine(Path.GetTempPath(), $"coding-instructions-{Guid.NewGuid():N}.md");
        await File.WriteAllTextAsync(path, instructions);
        Console.WriteLine(path);

        IReadOnlyList<string> prompts = await CodingPromptProvider.BuildAsync(path, CancellationToken.None);

        Assert.AreEqual(CodingPromptProvider.CodePrompt, prompts[1]);
    }
}
