using dotnetCampus.UITest.WPF;
using LightTextEditorPlus.Core.Document;
using LightTextEditorPlus.Core.Primitive.Collections;
using LightTextEditorPlus.Document.Decorations;
using MSTest.Extensions.Contracts;

namespace LightTextEditorPlus.Tests.Document.Decorations;

[TestClass()]
public class TextEditorDecorationHelperTest
{
    [UIContractTestCase]
    public void TestSplitContinuousTextDecorationCharData()
    {
        "传入三个字符，首个字符和其他字符的字号不相同，为这三个字符添加波浪线，可以枚举出两个分割集，首个分割集包含一个字符，末个分割集包含两个字符".Test(() =>
        {
            var waveLineTextEditorDecoration = new WaveLineTextEditorDecoration();
            TextEditor textEditor = new TextEditor();
            var runProperty1 = textEditor.CreateRunProperty(property => property with
            {
                DecorationCollection = waveLineTextEditorDecoration
            });
            var runProperty2 = textEditor.CreateRunProperty(property => property with
            {
                DecorationCollection = waveLineTextEditorDecoration,
                FontSize = 90,
            });

            var charDataList = new[]
            {
                new CharData(new SingleCharObject('1'), runProperty1),
                new CharData(new SingleCharObject('2'), runProperty2),
                new CharData(new SingleCharObject('3'), runProperty2),
            };
            var listSpan = new TextReadOnlyListSpan<CharData>(charDataList);
            List<DecorationSplitResult> decorationSplitResults = TextEditorDecorationHelper.SplitContinuousTextDecorationCharData(listSpan).ToList();
        });
    }
}