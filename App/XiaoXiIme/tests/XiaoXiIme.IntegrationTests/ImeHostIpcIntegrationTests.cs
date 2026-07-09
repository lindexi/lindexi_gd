using XiaoXiIme.Foundation;
using XiaoXiIme.ImeHost;
using XiaoXiIme.ImeIpc;

namespace XiaoXiIme.IntegrationTests;

public class ImeHostIpcIntegrationTests
{
    [Fact(Timeout = 5_000)]
    public async Task IpcClient_ProcessesKeysThroughImeHostService()
    {
        var options = new XiaoXiImeIpcOptions($"XiaoXiIme_Test_{Guid.NewGuid():N}");
        using var host = new ImeHostService(options);
        using var client = new XiaoXiImeIpcClient(options);

        host.Start();

        await client.ConnectAsync();
        await client.ProcessKeyAsync(ImeKey.FromCharacter('n'));
        var composingResult = await client.ProcessKeyAsync(ImeKey.FromCharacter('i'));

        Assert.True(composingResult.Handled);
        Assert.True(composingResult.Snapshot.IsComposing);
        Assert.Equal("ni", composingResult.Snapshot.Composition.Reading);
        Assert.Equal("你", composingResult.Snapshot.Candidates[0].Text);

        var commitResult = await client.ProcessKeyAsync(new ImeKey(ImeKeyKind.Space));

        Assert.True(commitResult.Handled);
        Assert.Equal("你", commitResult.CommitText);
        Assert.False(commitResult.Snapshot.IsComposing);

        var snapshot = await client.GetSnapshotAsync();

        Assert.False(snapshot.IsComposing);
        Assert.Empty(snapshot.Candidates);
    }

    [Fact(Timeout = 5_000)]
    public async Task IpcClient_GetsUiStateAndHostStatusThroughImeHostService()
    {
        var options = new XiaoXiImeIpcOptions($"XiaoXiIme_Test_{Guid.NewGuid():N}");
        using var host = new ImeHostService(options);
        using var client = new XiaoXiImeIpcClient(options);

        host.Start();

        var status = await client.GetHostStatusAsync();
        await client.ProcessKeyAsync(ImeKey.FromCharacter('n'));
        var result = await client.ProcessKeyAsync(ImeKey.FromCharacter('i'));
        var uiState = await client.GetUiStateAsync();

        Assert.True(status.IsRunning);
        Assert.True(result.Snapshot.IsComposing);
        Assert.True(uiState.CandidateWindowVisible);
        Assert.Equal("ni", uiState.Composition.Reading);
        Assert.Equal("你", uiState.Candidates[0].Text);
        Assert.Equal(uiState.Candidates.Count, uiState.CandidateWindow.PageSize);
        Assert.Equal(ImeGuidelineLevel.Reading, uiState.Guideline.Level);
    }

}

