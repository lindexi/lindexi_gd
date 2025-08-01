using MSTest.Extensions.Contracts;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive;
using LightTextEditorPlus.Core.TestsFramework;

namespace LightTextEditorPlus.Core.Tests.Document;

[TestClass]
public class LayoutOnlyRunPropertyTest
{
    [ContractTestCase]
    public void DefaultFontName()
    {
        "创建默认的文本框，默认是未定义的字体名".Test(() =>
        {
            // Arrange
            var textEditorCore = TestHelper.GetTextEditorCore();

            // Action
            var runProperty = textEditorCore.DocumentManager.StyleRunProperty;

            // Assert
            Assert.AreEqual(FontName.DefaultNotDefineFontName, runProperty.FontName);
            Assert.AreEqual(true, runProperty.FontName.IsNotDefineFontName);
        });
    }
}
