//using LightTextEditorPlus.Core.Carets;
//using LightTextEditorPlus.Core.Document.Segments;
//using MSTest.Extensions.Contracts;

//namespace LightTextEditorPlus.Core.Tests;

//[TestClass]
//public class ParagraphDataTest
//{
//    [ContractTestCase]
//    public void GetRunIndex()
//    {
//        "对段落里包含三个字符串，给定坐标是 1 获取取段落的 RunIndex 可以获取到首个".Test(() =>
//        {
//            // Arrange
//            var textEditor = TestHelper.GetTextEditorCore();
//            textEditor.AppendText("123");
//            var paragraphDataResult = textEditor.DocumentManager.TextRunManager.ParagraphManager.GetHitParagraphData(new CaretOffset(0));
//            var paragraphData = paragraphDataResult.ParagraphData;

//            // Action
//            var runIndex = paragraphData.GetRunIndex(new ParagraphOffset(1));

//            // Assert
//            Assert.AreEqual(0, runIndex.ParagraphIndex);
//            Assert.AreEqual(1, runIndex.HitRunIndex);
//        });

//        "给定段落坐标是 0 获取段落的 RunIndex 可以获取到首个".Test(() =>
//        {
//            // Arrange
//            var textEditor = TestHelper.GetTextEditorCore();
//            textEditor.AppendText("123");

//            // Action
//            var paragraphDataResult = textEditor.DocumentManager.TextRunManager.ParagraphManager.GetHitParagraphData(new CaretOffset(0));
//            var paragraphData = paragraphDataResult.ParagraphData;
//            var runIndex = paragraphData.GetRunIndex(new ParagraphOffset(0));

//            // Assert
//            Assert.AreEqual(0, runIndex.ParagraphIndex);
//        });
//    }
//}