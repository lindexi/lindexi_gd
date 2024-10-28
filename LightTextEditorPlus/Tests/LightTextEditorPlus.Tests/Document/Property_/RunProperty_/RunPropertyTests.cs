using System.Windows;
using System.Windows.Media;

using dotnetCampus.UITest.WPF;

using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Platform;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Document;

using MSTest.Extensions.Contracts;

namespace LightTextEditorPlus.Tests.Document;

[TestClass()]
public class RunPropertyTests
{
    [UIContractTestCase]
    public void EqualsBackground()
    {
        "两个 RunProperty 的 Background 不相同，则判断相等结果为不相等".Test(() =>
        {
            // Arrange
            // Action
            var runProperty1 = CreateRunProperty() with
            {
                Background = new ImmutableBrush(Brushes.AliceBlue)
            };
            var runProperty2 = CreateRunProperty() with
            {
                Background = new ImmutableBrush(Brushes.AntiqueWhite)
            };

            // Assert
            Assert.AreNotEqual(runProperty1, runProperty2);

            var flattenRunProperty = runProperty1.ToFlattenRunProperty();
            Assert.AreEqual(runProperty1, flattenRunProperty);
            Assert.AreNotEqual(runProperty2, flattenRunProperty);
        });
    }

    [UIContractTestCase]
    public void EqualsStretch()
    {
        "两个 RunProperty 的 Stretch 不相同，则判断相等结果为不相等".Test(() =>
        {
            // Arrange
            // Action
            var runProperty1 = CreateRunProperty() with
            {
                Stretch = FontStretches.SemiExpanded
            };
            var runProperty2 = CreateRunProperty() with
            {
                Stretch = FontStretches.Normal
            };

            // Assert
            Assert.AreNotEqual(runProperty1, runProperty2);

            var flattenRunProperty = runProperty1.ToFlattenRunProperty();
            Assert.AreEqual(runProperty1, flattenRunProperty);
            Assert.AreNotEqual(runProperty2, flattenRunProperty);
        });
    }

    [UIContractTestCase]
    public void EqualsFontStyle()
    {
        "两个 RunProperty 的 FontStyle 不相同，则判断相等结果为不相等".Test(() =>
        {
            // Arrange
            // Action
            var runProperty1 = CreateRunProperty() with
            {
                FontStyle = FontStyles.Normal
            };
            var runProperty2 = CreateRunProperty() with
            {
                FontStyle = FontStyles.Italic
            };

            // Assert
            Assert.AreNotEqual(runProperty1, runProperty2);

            var flattenRunProperty = runProperty1.ToFlattenRunProperty();
            Assert.AreEqual(runProperty1, flattenRunProperty);
            Assert.AreNotEqual(runProperty2, flattenRunProperty);
        });
    }

    [UIContractTestCase]
    public void EqualsFontWeight()
    {
        "两个 RunProperty 的 FontWeight 不相同，则判断相等结果为不相等".Test(() =>
        {
            // Arrange
            // Action
            var runProperty1 = CreateRunProperty() with
            {
                FontWeight = FontWeights.Bold
            };
            var runProperty2 = CreateRunProperty() with
            {
                FontWeight = FontWeights.ExtraBlack
            };

            // Assert
            Assert.AreNotEqual(runProperty1, runProperty2);

            var flattenRunProperty = runProperty1.ToFlattenRunProperty();
            Assert.AreEqual(runProperty1, flattenRunProperty);
            Assert.AreNotEqual(runProperty2, flattenRunProperty);
        });
    }

    [UIContractTestCase]
    public void EqualsOpacity()
    {
        "两个 RunProperty 的 Opacity 不相同，则判断相等结果为不相等".Test(() =>
        {
            // Arrange
            // Action
            var runProperty1 = CreateRunProperty() with
            {
                Opacity = 0.9
            };
            var runProperty2 = CreateRunProperty() with
            {
                Opacity = 0.2
            };

            // Assert
            Assert.AreNotEqual(runProperty1, runProperty2);

            var flattenRunProperty = runProperty1.ToFlattenRunProperty();
            Assert.AreEqual(runProperty1, flattenRunProperty);
            Assert.AreNotEqual(runProperty2, flattenRunProperty);
        });
    }

    [UIContractTestCase]
    public void EqualsForeground()
    {
        "两个 RunProperty 的 Foreground 不相同，则判断相等结果为不相等".Test(() =>
        {
            // Arrange
            // Action
            var runProperty1 = CreateRunProperty() with
            {
                Foreground = new ImmutableBrush(Brushes.AliceBlue)
            };
            var runProperty2 = CreateRunProperty() with
            {
                Foreground = new ImmutableBrush(Brushes.AntiqueWhite)
            };

            // Assert
            Assert.AreNotEqual(runProperty1, runProperty2);

            var flattenRunProperty = runProperty1.ToFlattenRunProperty();
            Assert.AreEqual(runProperty1, flattenRunProperty);
            Assert.AreNotEqual(runProperty2, flattenRunProperty);
        });
    }

    [UIContractTestCase]
    public void EqualsFontSize()
    {
        "两个 RunProperty 的大小不相同，则判断相等结果为不相等".Test(() =>
        {
            // Arrange
            // Action
            var runProperty1 = CreateRunProperty() with
            {
                FontSize = 100
            };
            var runProperty2 = CreateRunProperty() with
            {
                FontSize = 200
            };

            // Assert
            Assert.AreNotEqual(runProperty1, runProperty2);

            var flattenRunProperty = runProperty1.ToFlattenRunProperty();
            Assert.AreEqual(runProperty1, flattenRunProperty);
            Assert.AreNotEqual(runProperty2, flattenRunProperty);
        });
    }

    [UIContractTestCase]
    public void EqualsFontName()
    {
        "两个 RunProperty 的字体名不相同，则判断相等结果为不相等".Test(() =>
        {
            // Arrange
            // Action
            var runProperty1 = CreateRunProperty() with
            {
                FontName = new FontName("A")
            };
            var runProperty2 = CreateRunProperty() with
            {
                FontName = new FontName("B")
            };

            // Assert
            Assert.AreNotEqual(runProperty1, runProperty2);

            var flattenRunProperty = runProperty1.ToFlattenRunProperty();
            Assert.AreEqual(runProperty1, flattenRunProperty);
        });
    }

    private RunProperty CreateRunProperty()
    {
        using var context = TestFramework.CreateTextEditorInNewWindow();
        var textEditor = context.TextEditor;
        IPlatformRunPropertyCreator platformRunPropertyCreator = textEditor.TextEditorPlatformProvider.GetPlatformRunPropertyCreator();
        var runProperty = platformRunPropertyCreator.GetDefaultRunProperty();
        return (RunProperty) runProperty;
        //return (RunProperty) platformRunPropertyCreator.BuildNewProperty(property => config((RunProperty) property), runProperty);
    }

    [UIContractTestCase]
    public void EqualsTest()
    {
        "设置两个字符的字符属性的字体名不相同，可以返回是两个不同的字符属性".Test(() =>
        {
            // Arrange
            using var context = TestFramework.CreateTextEditorInNewWindow();
            var textEditor = context.TextEditor;
            // 放入两个字符，用来测试
            textEditor.TextEditorCore.AppendText("12");

            // Action
            // 设置两个字符的字符属性的字体名不相同
            textEditor.SetRunProperty(property => property with { FontName = new FontName("A") }, new Selection(new CaretOffset(0), 1));
            textEditor.SetRunProperty(property => property with{ FontName = new FontName("B") }, new Selection(new CaretOffset(1), 1));

            // Assert
            var runPropertyList = textEditor.TextEditorCore.DocumentManager.GetRunPropertyRange(new Selection(new CaretOffset(0), 2)).ToList();

            Assert.AreEqual(2, runPropertyList.Count);

            Assert.AreEqual(false, runPropertyList[0].Equals(runPropertyList[1]));

            Assert.AreNotEqual(runPropertyList[0], runPropertyList[1]);
        });
    }
}