using AgentLib;
using CoursewarePptxGeneratorWpfDemo.Services;
using PptxGenerator;

namespace CoursewarePptxGeneratorWpfDemo.Tests.Fakes;

internal sealed class FailingSlideChatManagerFactory : ISlideChatManagerFactory
{
    public Task<SlideChatManager> CreateAsync(
        SlideChatManagerFactoryOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        throw new InvalidOperationException("聊天管理器初始化失败。");
    }

    public SlideChatManager CreateFallback(SlideChatManagerFactoryOptions? options = null)
    {
        return new FakeSlideChatManagerFactory().CreateFallback(options);
    }
}
