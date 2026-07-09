using AgentLib;
using CoursewarePptxGeneratorWpfDemo.Services;
using PptxGenerator;

namespace CoursewarePptxGeneratorWpfDemo.Tests.Fakes;

internal sealed class FailingSlideChatManagerFactory : ISlideChatManagerFactory
{
    public Task<SlideChatManager> CreateAsync()
    {
        throw new InvalidOperationException("聊天管理器初始化失败。");
    }
}
