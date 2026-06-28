using System.Xml.Linq;
using PptxGenerator.Models;
using PptxGenerator.Streaming;

namespace PptxGenerator.Tests.Streaming;

[TestClass]
public sealed class SlideMlStreamingMergerTests
{
    [TestMethod]
    public void AttributeMerge_ExplicitOverridesExisting()
    {
        // Arrange
        var merger = new SlideMlStreamingMerger();
        var context = new SlideMlPipelineContext();

        // Act
        merger.AcceptFragment("<Panel Id=\"card\" X=\"0\" Y=\"0\" Width=\"100\" Height=\"50\"/>", context);
        merger.AcceptFragment("<Panel Id=\"card\" Width=\"200\"/>", context);

        // Assert
        var xml = merger.GetMergedXml();
        Assert.IsTrue(xml.Contains("Width=\"200\""), "Width 应被覆盖为 200");
        Assert.IsTrue(xml.Contains("Y=\"0\""), "Y 应保留原有值 0");
    }

    [TestMethod]
    public void AttributeMerge_UnspecifiedPreserved()
    {
        // Arrange
        var merger = new SlideMlStreamingMerger();
        var context = new SlideMlPipelineContext();

        // Act
        merger.AcceptFragment("<Rect Id=\"bg\" Fill=\"#FF0000\" Width=\"100\" Height=\"50\"/>", context);
        merger.AcceptFragment("<Rect Id=\"bg\" Height=\"80\"/>", context);

        // Assert
        var xml = merger.GetMergedXml();
        Assert.IsTrue(xml.Contains("Fill=\"#FF0000\""), "Fill 应保留原有值");
        Assert.IsTrue(xml.Contains("Height=\"80\""), "Height 应被覆盖为 80");
    }

    [TestMethod]
    public void ChildrenMerge_NewElementAppended()
    {
        // Arrange
        var merger = new SlideMlStreamingMerger();
        var context = new SlideMlPipelineContext();

        // Act
        merger.AcceptFragment("<Page><Panel Id=\"parent\"><Rect Id=\"card1\"/><Rect Id=\"card2\"/></Panel></Page>", context);
        merger.AcceptFragment("<Panel Id=\"parent\"><Rect Id=\"card3\"/></Panel>", context);

        // Assert
        var xml = merger.GetMergedXml();
        var doc = XDocument.Parse(xml);
        var ids = doc.Root!
            .Element("Panel")!
            .Elements()
            .Select(e => e.Attribute("Id")?.Value)
            .ToList();

        CollectionAssert.AreEqual(new[] { "card1", "card2", "card3" }, ids);
    }

    [TestMethod]
    public void ChildrenMerge_AnchorReorders()
    {
        // Arrange
        var merger = new SlideMlStreamingMerger();
        var context = new SlideMlPipelineContext();

        // Act
        merger.AcceptFragment("<Page><Panel Id=\"parent\"><Rect Id=\"card1\"/><Rect Id=\"card2\"/><Rect Id=\"card3\"/></Panel></Page>", context);
        merger.AcceptFragment("<Panel Id=\"parent\"><Rect Id=\"card4\"/><Rect Id=\"card2\"/></Panel>", context);

        // Assert
        var xml = merger.GetMergedXml();
        var doc = XDocument.Parse(xml);
        var ids = doc.Root!
            .Element("Panel")!
            .Elements()
            .Select(e => e.Attribute("Id")?.Value)
            .ToList();

        CollectionAssert.AreEqual(new[] { "card1", "card4", "card2", "card3" }, ids);
    }

    [TestMethod]
    public void ChildrenMerge_PExceedsLength_AppendsToEnd()
    {
        // Arrange
        var merger = new SlideMlStreamingMerger();
        var context = new SlideMlPipelineContext();

        // Act
        merger.AcceptFragment("<Page><Panel Id=\"parent\"><Rect Id=\"card1\"/><Rect Id=\"card4\"/><Rect Id=\"card2\"/><Rect Id=\"card3\"/></Panel></Page>", context);
        merger.AcceptFragment("<Panel Id=\"parent\"><Rect Id=\"card3\"/><Rect Id=\"card2\"/></Panel>", context);

        // Assert
        var xml = merger.GetMergedXml();
        var doc = XDocument.Parse(xml);
        var ids = doc.Root!
            .Element("Panel")!
            .Elements()
            .Select(e => e.Attribute("Id")?.Value)
            .ToList();

        CollectionAssert.AreEqual(new[] { "card1", "card4", "card3", "card2" }, ids);
    }

    [TestMethod]
    public void ContainerNoChildren_PreservesExistingChildren()
    {
        // Arrange
        var merger = new SlideMlStreamingMerger();
        var context = new SlideMlPipelineContext();

        // Act
        merger.AcceptFragment("<Page><Panel Id=\"parent\"><Rect Id=\"child1\"/><Rect Id=\"child2\"/></Panel></Page>", context);
        merger.AcceptFragment("<Panel Id=\"parent\" Background=\"#FF0000\"/>", context);

        // Assert
        var xml = merger.GetMergedXml();
        var doc = XDocument.Parse(xml);
        var panel = doc.Root!.Element("Panel")!;
        Assert.IsNotNull(panel);
        Assert.AreEqual("#FF0000", panel.Attribute("Background")?.Value);
        var childIds = panel.Elements()
            .Select(e => e.Attribute("Id")?.Value)
            .ToList();
        CollectionAssert.AreEqual(new[] { "child1", "child2" }, childIds);
    }

    [TestMethod]
    public void Remove_ExistingElement_RemovesFromTree()
    {
        // Arrange
        var merger = new SlideMlStreamingMerger();
        var context = new SlideMlPipelineContext();

        // Act
        merger.AcceptFragment("<Page><Panel Id=\"p1\"><Rect Id=\"r1\"/><Rect Id=\"r2\"/></Panel></Page>", context);
        merger.AcceptFragment("<Remove TargetId=\"r1\"/>", context);

        // Assert
        var xml = merger.GetMergedXml();
        Assert.IsFalse(xml.Contains("r1"), "r1 应被移除");
        Assert.IsTrue(xml.Contains("r2"), "r2 应保留");
    }

    [TestMethod]
    public void Remove_NonExistingTarget_WarningAndIgnore()
    {
        // Arrange
        var merger = new SlideMlStreamingMerger();
        var context = new SlideMlPipelineContext();

        // Act
        merger.AcceptFragment("<Page><Panel Id=\"p1\"/></Page>", context);
        merger.AcceptFragment("<Remove TargetId=\"nonexistent\"/>", context);

        // Assert
        Assert.IsTrue(context.Warnings.Any(w => w.Contains("nonexistent")), "应产生包含 nonexistent 的警告");
        var xml = merger.GetMergedXml();
        Assert.IsFalse(string.IsNullOrWhiteSpace(xml), "合并结果仍应有效");
    }

    [TestMethod]
    public void DanglingElement_RegisteredButNotRendered()
    {
        // Arrange
        var merger = new SlideMlStreamingMerger();
        var context = new SlideMlPipelineContext();

        // Act
        merger.AcceptFragment("<Rect Id=\"template\" Fill=\"#0000FF\"/>", context);
        merger.AcceptFragment("<Page/>", context);

        // Assert
        var xml = merger.GetMergedXml();
        var doc = XDocument.Parse(xml);
        Assert.AreEqual("Page", doc.Root!.Name.LocalName);
        Assert.IsNull(doc.Root.Element("Rect"), "Page 根元素下不应包含 template 悬空元素");
    }

    [TestMethod]
    public void StyleFrom_CopiesSourceAttributes()
    {
        // Arrange
        var merger = new SlideMlStreamingMerger();
        var context = new SlideMlPipelineContext();

        // Act
        merger.AcceptFragment("<Rect Id=\"template\" Fill=\"#FF0000\" Width=\"100\" Height=\"50\"/>", context);
        merger.AcceptFragment("<Page><Rect Id=\"card1\" StyleFrom=\"template\" X=\"0\" Y=\"0\"/></Page>", context);

        // Assert
        var xml = merger.GetMergedXml();
        var doc = XDocument.Parse(xml);
        var card1 = doc.Root!
            .Elements()
            .First(e => e.Attribute("Id")?.Value == "card1");

        Assert.AreEqual("#FF0000", card1.Attribute("Fill")?.Value, "应继承 Fill");
        Assert.AreEqual("100", card1.Attribute("Width")?.Value, "应继承 Width");
        Assert.AreEqual("50", card1.Attribute("Height")?.Value, "应继承 Height");
        Assert.AreEqual("0", card1.Attribute("X")?.Value, "应保留自身 X");
        Assert.AreEqual("0", card1.Attribute("Y")?.Value, "应保留自身 Y");
    }

    [TestMethod]
    public void StyleFrom_SourceNotFound_ErrorAndContinue()
    {
        // Arrange
        var merger = new SlideMlStreamingMerger();
        var context = new SlideMlPipelineContext();

        // Act
        merger.AcceptFragment("<Page/>", context);
        merger.AcceptFragment("<Page><Rect Id=\"card1\" StyleFrom=\"notexist\" X=\"0\"/></Page>", context);

        // Assert
        Assert.IsTrue(context.Errors.Any(e => e.Contains("notexist")), "应产生包含 notexist 的错误");
    }

    [TestMethod]
    public void Error_XmlFormatError_SkipsFragment()
    {
        // Arrange
        var merger = new SlideMlStreamingMerger();
        var context = new SlideMlPipelineContext();

        // Act
        merger.AcceptFragment("<Page/>", context);
        merger.AcceptFragment("<Panel Id=\"bad\"", context);

        // Assert
        Assert.IsTrue(context.Errors.Count > 0, "应产生 XML 格式错误");
        var xml = merger.GetMergedXml();
        var doc = XDocument.Parse(xml);
        Assert.AreEqual("Page", doc.Root!.Name.LocalName, "仅有 Page 根元素");
    }

    [TestMethod]
    public void Error_MissingId_SkipsElement()
    {
        // Arrange
        var merger = new SlideMlStreamingMerger();
        var context = new SlideMlPipelineContext();

        // Act
        merger.AcceptFragment("<Page/>", context);
        merger.AcceptFragment("<Page><Rect Fill=\"#FF0000\"/></Page>", context);

        // Assert
        Assert.IsTrue(context.Errors.Count > 0, "缺少 Id 的 Rect 应产生错误");
    }

    [TestMethod]
    public void Error_DuplicateIdInSameFragment_Error()
    {
        // Arrange
        var merger = new SlideMlStreamingMerger();
        var context = new SlideMlPipelineContext();

        // Act
        merger.AcceptFragment("<Page/>", context);
        merger.AcceptFragment("<Page><Panel Id=\"dup\"><Rect Id=\"dup\"/></Panel></Page>", context);

        // Assert
        Assert.IsTrue(context.Errors.Count > 0, "同片段内重复 Id 应产生错误");
    }

    [TestMethod]
    public void Reset_ClearsState()
    {
        // Arrange
        var merger = new SlideMlStreamingMerger();
        var context = new SlideMlPipelineContext();

        // Act
        merger.AcceptFragment("<Page/>", context);
        merger.Reset();

        // Assert
        var xml = merger.GetMergedXml();
        Assert.IsTrue(string.IsNullOrWhiteSpace(xml), "重置后应返回空或空白字符串");
    }

    [TestMethod]
    public void PageMerge_AttributesMerged()
    {
        // Arrange
        var merger = new SlideMlStreamingMerger();
        var context = new SlideMlPipelineContext();

        // Act
        merger.AcceptFragment("<Page Background=\"#FFFFFF\"/>", context);
        merger.AcceptFragment("<Page Background=\"#F5F5F5\"/>", context);

        // Assert
        var xml = merger.GetMergedXml();
        var doc = XDocument.Parse(xml);
        Assert.AreEqual("Page", doc.Root!.Name.LocalName);
        Assert.AreEqual("#F5F5F5", doc.Root.Attribute("Background")?.Value, "Background 应被覆盖为 F5F5F5");
    }
}
