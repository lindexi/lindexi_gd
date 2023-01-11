using LightTextEditorPlus.Core.Document;

using MSTest.Extensions.Contracts;

namespace LightTextEditorPlus.Core.Tests.Document;

[TestClass()]
public class TextSpanCharObjectTests
{
    [ContractTestCase()]
    public void IsContinuousNextCharObjectTest()
    {
        "使用相同的字符串，创建相邻的两个 TextSpanCharObject 对象，可以成功获取判断两个对象是相邻的".Test(() =>
        {
            var text = "123";
            var textSpanCharObject1 = new TextSpanCharObject(text, 0);
            var textSpanCharObject2 = new TextSpanCharObject(text, 1);
            var textSpanCharObject3 = new TextSpanCharObject(text, 2);

            Assert.AreEqual(true, textSpanCharObject1.IsContinuousNextCharObject(textSpanCharObject2));
            Assert.AreEqual(false, textSpanCharObject2.IsContinuousNextCharObject(textSpanCharObject1));
            Assert.AreEqual(false, textSpanCharObject1.IsContinuousNextCharObject(textSpanCharObject3));
        });
    }
}