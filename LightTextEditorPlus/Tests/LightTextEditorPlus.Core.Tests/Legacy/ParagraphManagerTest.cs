//using LightTextEditorPlus.Core.Document;
//using MSTest.Extensions.Contracts;

//namespace LightTextEditorPlus.Core.Tests;

//[TestClass]
//public class ParagraphManagerTest
//{
//    [ContractTestCase]
//    public void SplitReplace()
//    {
//        "一共有三个 IImmutableRun 的段落，将中间一个 IImmutableRun 拆分替换为两个，可以按照顺序替换掉".Test(() =>
//        {
//            // Arrange
//            var textEditor = TestHelper.GetTextEditorCore();

//            var paragraphManager = textEditor.DocumentManager.TextRunManager.ParagraphManager;
//            var paragraphData = paragraphManager.CreateParagraphData();

//            // 一共有三个 IImmutableRun 的段落
//            var originRun0 = new TextRun("1");
//            var originRun1 = new TextRun("23");
//            var originRun2 = new TextRun("4");
//            paragraphData.AppendRun(new IImmutableRun[] { originRun0, originRun1, originRun2 });

//            // Action
//            // 将中间一个 IImmutableRun 拆分替换为两个
//            var firstRun = new TextRun("2");
//            var secondRun = new TextRun("3");

//            paragraphData.SplitReplace(1, firstRun, secondRun);

//            // Assert
//            var text = paragraphData.GetText();
//            Assert.AreEqual("1234", text);

//            var runList = paragraphData.GetRunList();
//            Assert.AreEqual(4, runList.Count);
//            Assert.AreSame(originRun0, runList[0]);
//            Assert.AreSame(firstRun, runList[1]);
//            Assert.AreSame(secondRun, runList[2]);
//            Assert.AreSame(originRun2, runList[3]);
//        });

//        "将第一个 TextRun 拆分替换为两个，可以按照顺序替换掉".Test(() =>
//        {
//            // Arrange
//            var textEditor = TestHelper.GetTextEditorCore();

//            var paragraphManager = textEditor.DocumentManager.TextRunManager.ParagraphManager;
//            var paragraphData = paragraphManager.CreateParagraphData();

//            // Action
//            paragraphData.AppendRun(new TextRun("123"));
//            // 将第一个 TextRun 拆分替换为两个
//            var firstRun = new TextRun("1");
//            var secondRun = new TextRun("23");
//            paragraphData.SplitReplace(0, firstRun, secondRun);

//            // Assert
//            var text = paragraphData.GetText();
//            Assert.AreEqual("123", text);
//        });
//    }
//}