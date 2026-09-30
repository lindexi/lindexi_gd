using XiaoXiIme.Foundation;

namespace XiaoXiIme.ImeModule.Tests;

public sealed class XiaoXiImeAdapterConfigurationTests
{
    [Fact]
    public void DefaultAdapterShouldBeImm32()
    {
        Assert.Equal(ImeAdapterKind.Imm32, XiaoXiImeAdapterConfiguration.DefaultAdapter);
    }

    [Theory]
    [InlineData(ImeAdapterKind.Imm32)]
    [InlineData(ImeAdapterKind.Tsf)]
    public void DefaultAdapterShouldBeConfigurable(ImeAdapterKind adapterKind)
    {
        var original = XiaoXiImeAdapterConfiguration.DefaultAdapter;
        try
        {
            XiaoXiImeAdapterConfiguration.DefaultAdapter = adapterKind;

            Assert.Equal(adapterKind, XiaoXiImeAdapterConfiguration.DefaultAdapter);
        }
        finally
        {
            XiaoXiImeAdapterConfiguration.DefaultAdapter = original;
        }
    }

    [Fact]
    public void DefaultAdapterShouldRejectUnknownValue()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            XiaoXiImeAdapterConfiguration.DefaultAdapter = (ImeAdapterKind) int.MaxValue);
    }
}