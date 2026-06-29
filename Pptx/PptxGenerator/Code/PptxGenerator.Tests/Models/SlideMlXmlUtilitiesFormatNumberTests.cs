using PptxGenerator.Models;

namespace PptxGenerator.Tests.Models;

[TestClass]
public sealed class SlideMlXmlUtilitiesFormatNumberTests
{
    [TestMethod]
    public void FormatNumber_Integer_NoDecimal()
    {
        Assert.AreEqual("100", SlideMlXmlUtilities.FormatNumber(100.0));
    }

    [TestMethod]
    public void FormatNumber_TwoDecimals_Formatted()
    {
        Assert.AreEqual("100.25", SlideMlXmlUtilities.FormatNumber(100.25));
    }

    [TestMethod]
    public void FormatNumber_ManyDecimals_Rounded()
    {
        Assert.AreEqual("100.26", SlideMlXmlUtilities.FormatNumber(100.2567));
    }

    [TestMethod]
    public void FormatNumber_Zero_Formatted()
    {
        Assert.AreEqual("0", SlideMlXmlUtilities.FormatNumber(0));
    }

    [TestMethod]
    public void FormatNumber_Negative_Formatted()
    {
        Assert.AreEqual("-50.5", SlideMlXmlUtilities.FormatNumber(-50.5));
    }

    [TestMethod]
    public void FormatNumber_LargeNumber_Formatted()
    {
        Assert.AreEqual("1280", SlideMlXmlUtilities.FormatNumber(1280));
    }

    [TestMethod]
    public void FormatNumber_TrailingZeros_Removed()
    {
        Assert.AreEqual("100.5", SlideMlXmlUtilities.FormatNumber(100.50));
    }
}
