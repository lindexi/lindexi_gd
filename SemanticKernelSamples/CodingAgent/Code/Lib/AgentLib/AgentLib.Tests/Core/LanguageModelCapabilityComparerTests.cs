using AgentLib.Core;
using AgentLib.Core.AgentApiManagers.Contexts;
using AgentLib.Core.AgentApiManagers.LanguageModelProviders;

using Microsoft.Extensions.AI;

namespace AgentLib.Tests.Core;

[TestClass]
public class LanguageModelCapabilityComparerTests
{
    private readonly LanguageModelCapabilityComparer _comparer = new();

    [TestMethod]
    [Description("比较器在两个模型都为 null 时应返回相等结果")]
    public void Compare_WhenBothModelsAreNull_ReturnsZero()
    {
        var result = _comparer.Compare(null, null);

        Assert.AreEqual(0, result);
    }

    [TestMethod]
    [Description("比较器在左侧模型非空而右侧模型为空时应判定左侧更大")]
    public void Compare_WhenOnlyLeftModelIsNotNull_ReturnsPositive()
    {
        ILanguageModel left = CreateLanguageModel(new ModelDefinition { ModelName = "left" });

        var result = _comparer.Compare(left, null);

        Assert.IsGreaterThan(0, result);
    }

    [TestMethod]
    [Description("比较器在只有一方具备能力画像时应优先选择具备能力画像的模型")]
    public void Compare_WhenOnlyOneModelHasCapabilities_PrefersModelWithCapabilities()
    {
        ILanguageModel left = CreateLanguageModel(new ModelDefinition
        {
            ModelName = "capable",
            Capabilities = CreateCapabilities()
        });
        ILanguageModel right = CreateLanguageModel(new ModelDefinition { ModelName = "plain" });

        var result = _comparer.Compare(left, right);

        Assert.IsGreaterThan(0, result);
    }

    [TestMethod]
    [Description("比较器在两个模型都没有能力画像时应优先比较上下文窗口大小")]
    public void Compare_WhenCapabilitiesAreMissing_PrefersLargerContextWindow()
    {
        ILanguageModel left = CreateLanguageModel(new ModelDefinition { ModelName = "large", ContextWindowSize = 32000, MaxOutputTokens = 1024 });
        ILanguageModel right = CreateLanguageModel(new ModelDefinition { ModelName = "small", ContextWindowSize = 16000, MaxOutputTokens = 4096 });

        var result = _comparer.Compare(left, right);

        Assert.IsGreaterThan(0, result);
    }

    [TestMethod]
    [Description("比较器在上下文窗口相同时应继续比较最大输出令牌数")]
    public void Compare_WhenContextWindowIsEqual_PrefersLargerMaxOutputTokens()
    {
        ILanguageModel left = CreateLanguageModel(new ModelDefinition { ModelName = "large-output", ContextWindowSize = 32000, MaxOutputTokens = 4096 });
        ILanguageModel right = CreateLanguageModel(new ModelDefinition { ModelName = "small-output", ContextWindowSize = 32000, MaxOutputTokens = 2048 });

        var result = _comparer.Compare(left, right);

        Assert.IsGreaterThan(0, result);
    }

    [TestMethod]
    [Description("比较器在两个模型都具备能力画像时应按输入模态优先级比较")]
    public void Compare_WhenInputCapabilitiesDiffer_PrefersHigherPriorityModality()
    {
        ILanguageModel left = CreateLanguageModel(new ModelDefinition
        {
            ModelName = "image",
            Capabilities = CreateCapabilities(input: new LlmModalityCapability { Text = true, Image = true })
        });
        ILanguageModel right = CreateLanguageModel(new ModelDefinition
        {
            ModelName = "text",
            Capabilities = CreateCapabilities(input: new LlmModalityCapability { Text = true })
        });

        var result = _comparer.Compare(left, right);

        Assert.IsGreaterThan(0, result);
    }

    [TestMethod]
    [Description("比较器在输入模态相同时应继续比较工具调用能力")]
    public void Compare_WhenInputCapabilitiesAreEqual_PrefersToolCallCapability()
    {
        ILanguageModel left = CreateLanguageModel(new ModelDefinition
        {
            ModelName = "tool",
            Capabilities = CreateCapabilities(toolCall: true, reasoning: false, responseFormat: false)
        });
        ILanguageModel right = CreateLanguageModel(new ModelDefinition
        {
            ModelName = "no-tool",
            Capabilities = CreateCapabilities(toolCall: false, reasoning: true, responseFormat: true)
        });

        var result = _comparer.Compare(left, right);

        Assert.IsGreaterThan(0, result);
    }

    [TestMethod]
    [Description("比较器在前置能力都相同时应优先非快速模型")]
    public void Compare_WhenCapabilitiesAreEquivalent_PrefersNonFlashModel()
    {
        ILanguageModel left = CreateLanguageModel(new ModelDefinition
        {
            ModelName = "standard",
            Capabilities = CreateCapabilities(isFlash: false)
        });
        ILanguageModel right = CreateLanguageModel(new ModelDefinition
        {
            ModelName = "flash",
            Capabilities = CreateCapabilities(isFlash: true)
        });

        var result = _comparer.Compare(left, right);

        Assert.IsGreaterThan(0, result);
    }

    private static ILanguageModel CreateLanguageModel(ModelDefinition modelDefinition)
    {
        return new TestLanguageModel(modelDefinition);
    }

    private static LlmModelCapabilities CreateCapabilities(
        LlmModalityCapability? input = null,
        bool toolCall = true,
        bool reasoning = false,
        bool responseFormat = false,
        bool isFlash = false)
    {
        return new LlmModelCapabilities
        {
            Input = input ?? new LlmModalityCapability { Text = true },
            ToolCall = toolCall,
            Reasoning = reasoning,
            ResponseFormat = responseFormat,
            IsFlash = isFlash
        };
    }

    private sealed class TestLanguageModel(ModelDefinition modelDefinition) : ILanguageModel
    {
        public ModelDefinition ModelDefinition { get; } = modelDefinition;

        public Task<IChatClient> GetChatClientAsync()
        {
            return Task.FromException<IChatClient>(new NotSupportedException("测试替身不提供聊天客户端。"));
        }
    }
}
