using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LightTextEditorPlus.Core.Carets;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.TestsFramework;

using MSTest.Extensions.Contracts;

namespace LightTextEditorPlus.Core.Tests.Document;

[TestClass]
public class MarkerTest
{
    [ContractTestCase]
    public void TestMarkerLayout()
    {
        "测试带项目符号的段落缓存布局".Test(() =>
        {
            // 输入两段文本，两段都带项目符号
            // 修改第一段文本，让第二段进入缓存布局
            TextEditorCore textEditor = TestHelper.GetLayoutTestTextEditor();
            // 配置带项目符号
            textEditor.DocumentManager.SetStyleParagraphProperty(textEditor.DocumentManager.StyleParagraphProperty with
            {
                Marker = new BulletMarker()
                {
                    MarkerText = "•",
                }
            });

            // 输入两段文本
            // 加入两段文本，用于测试
            textEditor.AppendText("abc\r\ndef");
            // 此时瞬间就进入了布局了
            // 编辑修改第一段文本，让第二段进入缓存布局
            textEditor.EditAndReplace("1", new Selection(new CaretOffset(3), 1));
        });
    }
}
