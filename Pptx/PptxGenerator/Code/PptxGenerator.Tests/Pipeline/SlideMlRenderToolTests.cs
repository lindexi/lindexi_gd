using AgentLib;
using PptxGenerator.Models;
using PptxGenerator.Rendering;

namespace PptxGenerator.Pipeline.Tests;

[TestClass]
public sealed class SlideMlRenderToolTests
{
    [TestMethod]
    public async Task ApplyRenderResultAsync_WaitsForCompleteDispatcherCommit()
    {
        var dispatcher = new GatedDispatcher();
        var tool = new SlideMlRenderTool(new UnusedRenderPipeline(), dispatcher);
        var result = CreateResult(new RecordingPreviewImage());
        var eventCount = 0;
        tool.SlideRendered += () =>
        {
            eventCount++;
            Assert.AreSame(result.PreviewImage, tool.LatestPreviewImage);
            Assert.AreEqual(result.InputXml, tool.LatestSlideXml);
            Assert.AreEqual(result.OutputXml, tool.LatestRenderedXml);
            Assert.AreEqual("warning", tool.LatestWarnings);
        };

        Task applyTask = tool.ApplyRenderResultAsync(result);
        await dispatcher.WaitForInvocationAsync().WaitAsync(TimeSpan.FromSeconds(2));

        Assert.IsFalse(applyTask.IsCompleted);
        Assert.AreEqual(0, eventCount);

        dispatcher.Resume();
        await applyTask.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.AreEqual(1, eventCount);
    }

    [TestMethod]
    public async Task CreatePreviewDataContentAsync_DoesNotDispatchImageSave()
    {
        var dispatcher = new CountingDispatcher();
        var image = new RecordingPreviewImage();
        var tool = new SlideMlRenderTool(new UnusedRenderPipeline(), dispatcher);
        await tool.ApplyRenderResultAsync(CreateResult(image));
        var invocationCountAfterCommit = dispatcher.InvocationCount;

        var content = await tool.CreatePreviewDataContentAsync();

        Assert.IsNotNull(content);
        Assert.AreEqual(1, image.SaveCount);
        Assert.AreEqual(invocationCountAfterCommit, dispatcher.InvocationCount);
    }

    private static SlideMlRenderResult CreateResult(IPreviewImage previewImage) => new()
    {
        InputXml = "<Page/>",
        OutputXml = "<Page RenderSize=\"1,1\"/>",
        Warnings = ["warning"],
        Errors = Array.Empty<string>(),
        PreviewImage = previewImage,
    };

    private sealed class UnusedRenderPipeline : ISlideMlRenderPipeline
    {
        public Task<SlideMlRenderResult> RenderAsync(string slideXml, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class RecordingPreviewImage : IPreviewImage
    {
        private static readonly byte[] PngHeader = [137, 80, 78, 71, 13, 10, 26, 10];

        public int SaveCount { get; private set; }

        public void Save(string filePath)
        {
            using var stream = File.Create(filePath);
            Save(stream);
        }

        public void Save(Stream stream)
        {
            SaveCount++;
            stream.Write(PngHeader);
        }
    }

    private sealed class CountingDispatcher : IMainThreadDispatcher
    {
        public int InvocationCount { get; private set; }

        public bool CheckAccess() => false;

        public async Task InvokeAsync(Func<Task> action)
        {
            InvocationCount++;
            await action().ConfigureAwait(false);
        }

        public async Task<T> InvokeAsync<T>(Func<Task<T>> action)
        {
            InvocationCount++;
            return await action().ConfigureAwait(false);
        }
    }

    private sealed class GatedDispatcher : IMainThreadDispatcher
    {
        private readonly TaskCompletionSource _invocationSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _resumeSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool CheckAccess() => false;

        public Task InvokeAsync(Func<Task> action) => InvokeCoreAsync(action);

        public Task<T> InvokeAsync<T>(Func<Task<T>> action) => InvokeCoreAsync(action);

        public Task WaitForInvocationAsync() => _invocationSource.Task;

        public void Resume() => _resumeSource.TrySetResult();

        private async Task InvokeCoreAsync(Func<Task> action)
        {
            _invocationSource.TrySetResult();
            await _resumeSource.Task.ConfigureAwait(false);
            await action().ConfigureAwait(false);
        }

        private async Task<T> InvokeCoreAsync<T>(Func<Task<T>> action)
        {
            _invocationSource.TrySetResult();
            await _resumeSource.Task.ConfigureAwait(false);
            return await action().ConfigureAwait(false);
        }
    }
}
