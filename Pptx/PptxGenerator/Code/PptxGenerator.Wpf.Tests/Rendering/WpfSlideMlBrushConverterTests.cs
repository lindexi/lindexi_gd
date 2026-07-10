using Microsoft.VisualStudio.TestTools.UnitTesting;
using PptxGenerator.Models.SlideDocuments;
using PptxGenerator.Rendering;
using System.Windows.Media;

namespace PptxGenerator.Wpf.Tests.Rendering;

[TestClass]
public sealed class WpfSlideMlBrushConverterTests
{
    [TestMethod(DisplayName = "转换六位颜色字符串时应返回不透明纯色画刷")]
    public void ConvertToColor_WhenSixDigitColorThenReturnsOpaqueColor()
    {
        var result = WpfSlideMlBrushConverter.ConvertToColor("#336699");

        Assert.IsTrue(result.success);
        Assert.AreEqual(0xFF, result.a);
        Assert.AreEqual(0x33, result.r);
        Assert.AreEqual(0x66, result.g);
        Assert.AreEqual(0x99, result.b);
    }

    [TestMethod(DisplayName = "转换三位颜色字符串时应扩展每个颜色通道")]
    public void ConvertToColor_WhenShortColorThenExpandsChannels()
    {
        var result = WpfSlideMlBrushConverter.ConvertToColor("#369");

        Assert.IsTrue(result.success);
        Assert.AreEqual(0xFF, result.a);
        Assert.AreEqual(0x33, result.r);
        Assert.AreEqual(0x66, result.g);
        Assert.AreEqual(0x99, result.b);
    }

    [TestMethod(DisplayName = "转换非法颜色字符串时应返回失败")]
    public void ConvertToColor_WhenInvalidColorThenReturnsFailure()
    {
        var result = WpfSlideMlBrushConverter.ConvertToColor("#XYZ");

        Assert.IsFalse(result.success);
    }

    [TestMethod(DisplayName = "创建纯色画刷时应保留颜色通道")]
    public void CreateWpfBrush_WhenSolidColorThenReturnsSolidColorBrush()
    {
        var brush = WpfSlideMlBrushConverter.CreateWpfBrush(new SlideMlSolidColorBrush("#80336699"));

        Assert.IsInstanceOfType<SolidColorBrush>(brush);
        var solidColorBrush = (SolidColorBrush)brush;
        Assert.AreEqual(Color.FromArgb(0x80, 0x33, 0x66, 0x99), solidColorBrush.Color);
    }
}
