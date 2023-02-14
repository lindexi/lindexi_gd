using System.Windows.Media;
using dotnetCampus.UITest.WPF;
using LightTextEditorPlus.Utils;
using MSTest.Extensions.Contracts;

namespace LightTextEditorPlus.Tests;

[TestClass]
public class ConverterTest
{
    [UIContractTestCase]
    public void Equals()
    {
        "给定两个颜色值相同的纯色画刷，返回相等".Test(() =>
        {
            var brush1 = new SolidColorBrush(Color.FromRgb(0x02, 0x03, 0x00));
            var brush2 = new SolidColorBrush(Color.FromRgb(0x02, 0x03, 0x00));

            var result = Converter.AreEquals(brush1, brush2);
            Assert.AreEqual(true, result);
        });

        "给定两个不相同的预设纯色画刷，返回不相等".Test(() =>
        {
            var brush1 = Brushes.White;
            var brush2 = Brushes.Black;

            var result = Converter.AreEquals(brush1, brush2);
            Assert.AreEqual(false, result);
        });

        "给定和两个 Null 的画刷，返回相等".Test(() =>
        {
            Brush brush1 = null;
            Brush brush2 = null;

            var result = Converter.AreEquals(brush1, brush2);
            Assert.AreEqual(true, result);
        });
        "给定和一个 Null 的画刷，一个预设的纯色，返回不相等".Test(() =>
        {
            Brush brush1 = null;
            var brush2 = Brushes.Black;

            var result = Converter.AreEquals(brush1, brush2);
            Assert.AreEqual(false, result);
        });

        "给定一个预设的纯色，和一个 Null 的画刷，返回不相等".Test(() =>
        {
            var brush1 = Brushes.Black;
            Brush brush2 = null;

            var result = Converter.AreEquals(brush1, brush2);
            Assert.AreEqual(false, result);
        });

        "给定两个预设的纯色，可以判断相等".Test(() =>
        {
            var brush1 = Brushes.Black;
            var brush2 = Brushes.Black;

            var result = Converter.AreEquals(brush1, brush2);
            Assert.AreEqual(true, result);
        });
    }
}