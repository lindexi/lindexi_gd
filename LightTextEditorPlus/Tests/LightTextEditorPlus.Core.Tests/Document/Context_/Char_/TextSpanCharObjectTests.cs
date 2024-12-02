using System.Globalization;
using LightTextEditorPlus.Core.Document;

using MSTest.Extensions.Contracts;

using static System.Net.Mime.MediaTypeNames;

namespace LightTextEditorPlus.Core.Tests.Document;

[TestClass()]
public class TextSpanCharObjectTests
{
    [ContractTestCase()]
    public void IsIncreasingContinuous()
    {
        "传入字面相同，但是实际对象不相同的字符串创建的相邻两个 TextSpanCharObject 对象，通过 IsIncreasingContinuous 判断是相同的一个字符串的下一个字符将失败".Test(() =>
        {
            var n = Random.Shared.Next();
            var text1 = "123" + n;
            var text2 = 123.ToString(CultureInfo.InvariantCulture) + n.ToString();
            var textSpanCharObject1 = new TextSpanCharObject(text1, 0);
            var textSpanCharObject2 = new TextSpanCharObject(text2, 1);
            Assert.AreEqual(false, textSpanCharObject1.IsIncreasingContinuous(textSpanCharObject2));
        });

        "传入在相同的字符串里面的相邻两个 TextSpanCharObject 对象，可以通过 IsIncreasingContinuous 判断是相同的一个字符串的下一个字符".Test(() =>
        {
            var text = "123";
            var textSpanCharObject1 = new TextSpanCharObject(text, 0);
            var textSpanCharObject2 = new TextSpanCharObject(text, 1);
            Assert.AreEqual(true, textSpanCharObject1.IsIncreasingContinuous(textSpanCharObject2));
        });
    }

    [ContractTestCase()]
    public void GetOriginText()
    {
        "可以从 TextSpanCharObject 的 GetOriginText 获取传入的原始字符串".Test(() =>
        {
            var text = "123";
            var textSpanCharObject = new TextSpanCharObject(text, 0);

            Assert.AreSame(text, textSpanCharObject.GetOriginText());
        });
    }

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