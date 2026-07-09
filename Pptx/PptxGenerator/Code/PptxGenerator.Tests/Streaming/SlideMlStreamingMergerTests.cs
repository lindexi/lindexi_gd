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
        merger.AcceptFragment("<Panel Id=\"card\" StyleId=\"card-style\" X=\"0\" Y=\"0\" Width=\"100\" Height=\"50\"/>", context);
        merger.AcceptFragment("<Panel Id=\"card\" Width=\"200\"/>", context);

        // Assert
        var xml = merger.GetMergedXml();
        Assert.Contains("Width=\"200\"", xml, "Width 应被覆盖为 200");
        Assert.Contains("Y=\"0\"", xml, "Y 应保留原有值 0");
    }

    [TestMethod]
    public void AttributeMerge_UnspecifiedPreserved()
    {
        // Arrange
        var merger = new SlideMlStreamingMerger();
        var context = new SlideMlPipelineContext();

        // Act
        merger.AcceptFragment("<Rect Id=\"bg\" StyleId=\"bg-style\" Fill=\"#FF0000\" Width=\"100\" Height=\"50\"/>", context);
        merger.AcceptFragment("<Rect Id=\"bg\" Height=\"80\"/>", context);

        // Assert
        var xml = merger.GetMergedXml();
        Assert.Contains("Fill=\"#FF0000\"", xml, "Fill 应保留原有值");
        Assert.Contains("Height=\"80\"", xml, "Height 应被覆盖为 80");
    }

    [TestMethod(DisplayName = "流式合并：空字符串清除 TextElement 的 Width 属性")]
    public void AttributeMerge_EmptyOptionalWidth_RemovesExistingWidth()
    {
        // Arrange
        var merger = new SlideMlStreamingMerger();
        var context = new SlideMlPipelineContext();

        // Act
        merger.AcceptFragment("<Page><TextElement Id=\"title\" X=\"80\" Y=\"48\" Width=\"420\" Text=\"SlideML 流式输出\" FontSize=\"36\"/></Page>", context);
        merger.AcceptFragment("<TextElement Id=\"title\" Width=\"\"/>", context);

        // Assert
        Assert.IsEmpty(context.Errors, "不应有错误");
        var xml = merger.GetMergedXml();
        var doc = XDocument.Parse(xml);
        var title = doc.Root!.Elements("TextElement").Single(e => e.Attribute("Id")?.Value == "title");

        Assert.IsNull(title.Attribute("Width"), "Width 应被清除，恢复为未设置状态");
        Assert.AreEqual("80", title.Attribute("X")?.Value, "未声明的 X 应保留");
        Assert.AreEqual("SlideML 流式输出", title.Attribute("Text")?.Value, "未声明的 Text 应保留");
    }

    [TestMethod(DisplayName = "流式合并：空字符串清除 X 后保留 HorizontalAlignment")]
    public void AttributeMerge_EmptyOptionalX_RemovesXAndKeepsAlignment()
    {
        // Arrange
        var merger = new SlideMlStreamingMerger();
        var context = new SlideMlPipelineContext();

        // Act
        merger.AcceptFragment("<Page><TextElement Id=\"title\" X=\"80\" Text=\"标题\"/></Page>", context);
        merger.AcceptFragment("<TextElement Id=\"title\" X=\"\" HorizontalAlignment=\"Center\"/>", context);

        // Assert
        Assert.IsEmpty(context.Errors, "不应有错误");
        var xml = merger.GetMergedXml();
        var doc = XDocument.Parse(xml);
        var title = doc.Root!.Elements("TextElement").Single(e => e.Attribute("Id")?.Value == "title");

        Assert.IsNull(title.Attribute("X"), "X 应被清除，让 HorizontalAlignment 能够生效");
        Assert.AreEqual("Center", title.Attribute("HorizontalAlignment")?.Value, "HorizontalAlignment 应被设置为 Center");
    }

    [TestMethod(DisplayName = "流式合并：Text 空字符串表示空文本而不是清除属性")]
    public void AttributeMerge_EmptyText_PreservesEmptyTextAttribute()
    {
        // Arrange
        var merger = new SlideMlStreamingMerger();
        var context = new SlideMlPipelineContext();

        // Act
        merger.AcceptFragment("<Page><TextElement Id=\"body\" Text=\"旧文本\" Width=\"300\"/></Page>", context);
        merger.AcceptFragment("<TextElement Id=\"body\" Text=\"\"/>", context);

        // Assert
        Assert.IsEmpty(context.Errors, "不应有错误");
        var xml = merger.GetMergedXml();
        var doc = XDocument.Parse(xml);
        var body = doc.Root!.Elements("TextElement").Single(e => e.Attribute("Id")?.Value == "body");

        Assert.IsNotNull(body.Attribute("Text"), "Text 属性不应被清除");
        Assert.AreEqual(string.Empty, body.Attribute("Text")?.Value, "Text 应被覆盖为空文本");
        Assert.AreEqual("300", body.Attribute("Width")?.Value, "未声明的 Width 应保留");
    }

    [TestMethod(DisplayName = "流式合并：子元素合并时空字符串清除继承样式属性")]
    public void AttributeMerge_ChildWithStyleFromAndEmptyWidth_RemovesInheritedWidth()
    {
        // Arrange
        var merger = new SlideMlStreamingMerger();
        var context = new SlideMlPipelineContext();

        // Act
        merger.AcceptFragment("<TextElement Id=\"title-template\" StyleId=\"title-style\" Width=\"420\" FontSize=\"36\"/>", context);
        merger.AcceptFragment("<Page><Panel Id=\"header\"><TextElement Id=\"title\" StyleFrom=\"title-style\" Width=\"\" Text=\"标题\"/></Panel></Page>", context);

        // Assert
        Assert.IsEmpty(context.Errors, "不应有错误");
        var xml = merger.GetMergedXml();
        var doc = XDocument.Parse(xml);
        var title = doc.Root!
            .Element("Panel")!
            .Elements("TextElement")
            .Single(e => e.Attribute("Id")?.Value == "title");

        Assert.IsNull(title.Attribute("Width"), "显式 Width 空字符串应清除 StyleFrom 继承来的 Width");
        Assert.AreEqual("36", title.Attribute("FontSize")?.Value, "未清除的样式属性应继续继承");
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
        Assert.DoesNotContain("r1", xml, "r1 应被移除");
        Assert.Contains("r2", xml, "r2 应保留");
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
    public void Remove_PageById_ClearsEntireDocument()
    {
        // Arrange
        var merger = new SlideMlStreamingMerger();
        var context = new SlideMlPipelineContext();

        // Act
        merger.AcceptFragment("<Page Id=\"my-page\"><Panel Id=\"p1\"><Rect Id=\"r1\"/></Panel></Page>", context);
        merger.AcceptFragment("<Remove TargetId=\"my-page\"/>", context);

        // Assert
        Assert.IsEmpty(context.Errors, "不应有错误");
        Assert.IsEmpty(context.Warnings, "不应有警告");
        var xml = merger.GetMergedXml();
        Assert.IsTrue(string.IsNullOrWhiteSpace(xml), "移除 Page 后 XML 应为空");
    }

    [TestMethod]
    public void Remove_PageById_AllowsReAddingNewPage()
    {
        // Arrange
        var merger = new SlideMlStreamingMerger();
        var context = new SlideMlPipelineContext();

        // Act — 先创建 Page，移除，再创建新 Page
        merger.AcceptFragment("<Page Id=\"old-page\"><Rect Id=\"r1\"/></Page>", context);
        merger.AcceptFragment("<Remove TargetId=\"old-page\"/>", context);
        merger.AcceptFragment("<Page Id=\"new-page\"><Rect Id=\"r2\"/></Page>", context);

        // Assert
        Assert.IsEmpty(context.Errors, "不应有错误");
        var xml = merger.GetMergedXml();
        var doc = XDocument.Parse(xml);
        Assert.AreEqual("new-page", doc.Root!.Attribute("Id")?.Value);
        Assert.Contains("r2", xml, "新 Page 的子元素应存在");
        Assert.DoesNotContain("old-page", xml, "旧 Page 不应残留");
        Assert.DoesNotContain("r1", xml, "旧 Page 的子元素不应残留");
    }

    [TestMethod]
    public void Remove_PageWithoutId_TargetNotFoundWarning()
    {
        // Arrange — Page 不带 Id 时不会被注册到 IdIndex，Remove 找不到目标
        var merger = new SlideMlStreamingMerger();
        var context = new SlideMlPipelineContext();

        // Act
        merger.AcceptFragment("<Page><Rect Id=\"r1\"/></Page>", context);
        merger.AcceptFragment("<Remove TargetId=\"some-page-id\"/>", context);

        // Assert
        Assert.IsTrue(context.Warnings.Any(w => w.Contains("some-page-id")), "应产生目标不存在的警告");
    }

    [TestMethod(DisplayName = "移除悬空元素后可以重新添加同 StyleId 的元素")]
    public void Remove_DanglingElement_AllowsReAddingSameStyleId()
    {
        // Arrange — 模拟先移除再添加的重置场景
        var merger = new SlideMlStreamingMerger();
        var context = new SlideMlPipelineContext();

        // Act — 添加悬空模板，再移除，再用相同 StyleId 添加新模板
        merger.AcceptFragment("<Rect Id=\"tmpl\" StyleId=\"tmpl-style\" Fill=\"#FF0000\" Width=\"100\" Height=\"50\"/>", context);
        merger.AcceptFragment("<Remove TargetId=\"tmpl\"/>", context);
        merger.AcceptFragment("<Rect Id=\"tmpl-v2\" StyleId=\"tmpl-style\" Fill=\"#00FF00\" Width=\"200\" Height=\"100\"/>", context);
        merger.AcceptFragment("<Page><Rect Id=\"card\" StyleFrom=\"tmpl-style\" X=\"0\" Y=\"0\"/></Page>", context);

        // Assert
        Assert.IsEmpty(context.Errors, "不应有错误");
        var xml = merger.GetMergedXml();
        var doc = XDocument.Parse(xml);
        var card = doc.Root!.Elements().First(e => e.Attribute("Id")?.Value == "card");
        Assert.AreEqual("#00FF00", card.Attribute("Fill")?.Value, "应从新模板的 StyleId 继承 Fill（而非旧模板 #FF0000）");
        Assert.AreEqual("200", card.Attribute("Width")?.Value, "应从新模板继承 Width");
        Assert.AreEqual("100", card.Attribute("Height")?.Value, "应从新模板继承 Height");
    }

    [TestMethod(DisplayName = "移除 Page 根元素时同时清空悬空元素列表")]
    public void Remove_PageRoot_ClearsDanglingElements()
    {
        // Arrange
        var merger = new SlideMlStreamingMerger();
        var context = new SlideMlPipelineContext();

        // Act — 添加悬空元素和 Page，然后移除 Page
        merger.AcceptFragment("<Rect Id=\"tmpl\" StyleId=\"tmpl-style\" Fill=\"#FF0000\"/>", context);
        merger.AcceptFragment("<Page Id=\"page1\"><Rect Id=\"card\" StyleFrom=\"tmpl-style\" X=\"0\" Y=\"0\"/></Page>", context);
        merger.AcceptFragment("<Remove TargetId=\"page1\"/>", context);

        // 移除 Page 后，DanglingElements 也应该被清空。重新添加同 StyleId 的悬空元素不应冲突
        merger.AcceptFragment("<Rect Id=\"tmpl-new\" StyleId=\"tmpl-style\" Fill=\"#00FF00\"/>", context);
        merger.AcceptFragment("<Page Id=\"page2\"><Rect Id=\"card2\" StyleFrom=\"tmpl-style\" X=\"0\" Y=\"0\"/></Page>", context);

        // Assert
        Assert.IsEmpty(context.Errors, "不应有错误");
        var xml = merger.GetMergedXml();
        var doc = XDocument.Parse(xml);
        Assert.AreEqual("page2", doc.Root!.Attribute("Id")?.Value);
        var card2 = doc.Root!.Elements().First(e => e.Attribute("Id")?.Value == "card2");
        Assert.AreEqual("#00FF00", card2.Attribute("Fill")?.Value, "应从新模板继承 Fill");
    }

    [TestMethod(DisplayName = "移除悬空元素后 StyleId 索引也被清理")]
    public void Remove_DanglingElement_ClearsStyleIdIndex()
    {
        // Arrange
        var merger = new SlideMlStreamingMerger();
        var context = new SlideMlPipelineContext();

        // Act — 添加悬空模板，移除，再添加同 StyleId 的模板（不应产生 StyleId 重复错误）
        merger.AcceptFragment("<Rect Id=\"tmpl-1\" StyleId=\"my-style\" Fill=\"#FF0000\"/>", context);
        merger.AcceptFragment("<Remove TargetId=\"tmpl-1\"/>", context);
        merger.AcceptFragment("<Rect Id=\"tmpl-2\" StyleId=\"my-style\" Fill=\"#0000FF\"/>", context);

        // Assert
        Assert.IsEmpty(context.Errors, "移除后重新添加同 StyleId 不应产生重复错误");
        Assert.IsEmpty(context.Warnings, "不应有警告");
    }

    [TestMethod(DisplayName = "子元素合并冲突移除时同时清理悬空元素列表")]
    public void MergeChildren_ConflictRemoval_ClearsDanglingElements()
    {
        // Arrange
        var merger = new SlideMlStreamingMerger();
        var context = new SlideMlPipelineContext();

        // Act — 先添加悬空元素，再在 Page 内创建同 Id 元素（此时悬空元素被提升为 Page 子元素），
        // 然后再用 MergeChildren 覆盖替换（冲突移除）
        merger.AcceptFragment("<Rect Id=\"card\" StyleId=\"card-style\" Fill=\"#FF0000\" Width=\"100\" Height=\"50\"/>", context);
        merger.AcceptFragment("<Page><Panel Id=\"panel\"><Rect Id=\"card\"/></Panel></Page>", context);
        // 此时 card 已在 Page 子树中，再发送同 Id 片段触发 MergeChildren 冲突移除
        merger.AcceptFragment("<Panel Id=\"panel\"><Rect Id=\"card\" Fill=\"#00FF00\" Width=\"200\"/></Panel>", context);

        // Assert
        Assert.IsEmpty(context.Errors, "不应有错误");
        var xml = merger.GetMergedXml();
        Assert.Contains("Fill=\"#00FF00\"", xml, "冲突替换后应使用新属性");
        Assert.Contains("Width=\"200\"", xml, "冲突替换后应使用新 Width");
    }

    [TestMethod(DisplayName = "移除 Page 树内带属性的元素后，重新添加同 Id 元素不含旧属性")]
    public void Remove_InPageTreeElement_ThenReAddSameId_ShouldNotRetainOldAttributes()
    {
        // Arrange
        var merger = new SlideMlStreamingMerger();
        var context = new SlideMlPipelineContext();

        // Act — 在 Page 下创建带阴影等属性的 Rect，移除，再添加同 Id 但没有任何阴影属性的 Rect
        merger.AcceptFragment(
            """
            <Page>
              <Panel Id="panel">
                <Rect Id="box" Width="200" Height="100" Fill="#FF0000" Shadow="2 2 4 #000000" />
                <Rect Id="other" Width="50" Height="50" Fill="#0000FF" />
              </Panel>
            </Page>
            """, context);
        merger.AcceptFragment("<Remove TargetId=\"box\"/>", context);
        merger.AcceptFragment(
            """
            <Panel Id="panel">
              <Rect Id="box" Width="300" Height="150" Fill="#00FF00" />
            </Panel>
            """, context);

        // Assert
        Assert.IsEmpty(context.Errors, "不应有错误");
        var xml = merger.GetMergedXml();
        var doc = XDocument.Parse(xml);
        var box = doc.Root!.Element("Panel")!.Elements().First(e => e.Attribute("Id")?.Value == "box");

        // 新元素应使用新属性
        Assert.AreEqual("300", box.Attribute("Width")?.Value, "Width 应为新值 300");
        Assert.AreEqual("150", box.Attribute("Height")?.Value, "Height 应为新值 150");
        Assert.AreEqual("#00FF00", box.Attribute("Fill")?.Value, "Fill 应为新值 #00FF00");

        // 旧元素的 Shadow 属性不应残留
        Assert.IsNull(box.Attribute("Shadow"), "移除后重新添加不应保留旧元素的 Shadow 属性");

        // other 元素应保留
        var other = doc.Root!.Element("Panel")!.Elements().First(e => e.Attribute("Id")?.Value == "other");
        Assert.IsNotNull(other, "other 元素应保留");
        Assert.AreEqual("#0000FF", other.Attribute("Fill")?.Value, "other 的 Fill 应保留");
    }

    [TestMethod]
    public void DanglingElement_RegisteredButNotRendered()
    {
        // Arrange
        var merger = new SlideMlStreamingMerger();
        var context = new SlideMlPipelineContext();

        // Act
        merger.AcceptFragment("<Rect Id=\"template\" StyleId=\"template-style\" Fill=\"#0000FF\"/>", context);
        merger.AcceptFragment("<Page/>", context);

        // Assert
        var xml = merger.GetMergedXml();
        var doc = XDocument.Parse(xml);
        Assert.AreEqual("Page", doc.Root!.Name.LocalName);
        Assert.IsNull(doc.Root.Element("Rect"), "Page 根元素下不应包含 template 悬空元素");
    }

    [TestMethod(DisplayName = "首片段为悬空样式元素：先暂存样式，后续 Page 成为根并可引用样式")]
    public void FirstFragment_DanglingStyleElement_PageBecomesRootAndCanReferenceStyle()
    {
        // Arrange
        var merger = new SlideMlStreamingMerger();
        var context = new SlideMlPipelineContext();

        // Act — 首个片段不是 Page，而是合法悬空样式元素
        merger.AcceptFragment("<Rect Id=\"card-template\" StyleId=\"card-style\" Fill=\"#FF0000\" Width=\"100\" Height=\"50\"/>", context);
        var xmlAfterDanglingFragment = merger.GetMergedXml();

        merger.AcceptFragment("<Page><Rect Id=\"card1\" StyleFrom=\"card-style\" X=\"10\" Y=\"20\"/></Page>", context);

        // Assert — 当前实现会将首个悬空元素暂存为内部根；Page 到来后降级为悬空样式源
        Assert.IsEmpty(context.Errors, "合法悬空样式元素不应产生错误");
        Assert.Contains("card-template", xmlAfterDanglingFragment, "Page 到来前，合并器内部状态会保留首个悬空样式元素");

        var xml = merger.GetMergedXml();
        var doc = XDocument.Parse(xml);
        Assert.AreEqual("Page", doc.Root!.Name.LocalName, "后续 Page 片段应成为最终可渲染根元素");
        Assert.IsNull(doc.Root.Element("Rect")?.Attribute("Id")?.Value == "card-template" ? doc.Root.Element("Rect") : null, "悬空样式元素不应作为 Page 子元素渲染");

        var card1 = doc.Root.Elements("Rect").Single(e => e.Attribute("Id")?.Value == "card1");
        Assert.AreEqual("#FF0000", card1.Attribute("Fill")?.Value, "Page 内元素应能通过 StyleFrom 继承首片段悬空样式");
        Assert.AreEqual("100", card1.Attribute("Width")?.Value, "Page 内元素应继承悬空样式 Width");
        Assert.AreEqual("50", card1.Attribute("Height")?.Value, "Page 内元素应继承悬空样式 Height");
        Assert.AreEqual("10", card1.Attribute("X")?.Value, "自身显式属性优先保留");
        Assert.AreEqual("20", card1.Attribute("Y")?.Value, "自身显式属性优先保留");
        Assert.IsNull(card1.Attribute("StyleFrom"), "StyleFrom 应在应用后被移除");
    }

    [TestMethod]
    public void StyleFrom_CopiesSourceAttributes()
    {
        // Arrange
        var merger = new SlideMlStreamingMerger();
        var context = new SlideMlPipelineContext();

        // Act
        merger.AcceptFragment("<Rect Id=\"template\" StyleId=\"template-style\" Fill=\"#FF0000\" Width=\"100\" Height=\"50\"/>", context);
        merger.AcceptFragment("<Page><Rect Id=\"card1\" StyleFrom=\"template-style\" X=\"0\" Y=\"0\"/></Page>", context);

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
        Assert.IsNotEmpty(context.Errors, "应产生 XML 格式错误");
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
        Assert.IsNotEmpty(context.Errors, "缺少 Id 的 Rect 应产生错误");
    }

    [TestMethod(DisplayName = "流式合并：TextElement 的 Span 子元素不要求 Id")]
    public void Merge_PageFragmentWithTextElementSpans_DoesNotRequireSpanId()
    {
        // Arrange
        var merger = new SlideMlStreamingMerger();
        var context = new SlideMlPipelineContext();

        merger.AcceptFragment("<Page><TextElement Id=\"title\" Text=\"Old\"/></Page>", context);

        // Act
        merger.AcceptFragment("<Page><TextElement Id=\"title\"><Span Text=\"新\"/><Span Text=\"标题\"/></TextElement></Page>", context);

        // Assert
        Assert.IsFalse(context.Errors.Any(e => e.Contains("元素缺少 Id") && e.Contains("Span")),
            $"Span 子元素不应要求 Id，实际错误: {string.Join("; ", context.Errors)}");

        var xml = merger.GetMergedXml();
        var doc = XDocument.Parse(xml);
        var textElement = doc.Root!.Elements("TextElement").Single();
        var spans = textElement.Elements("Span").ToList();

        Assert.AreEqual(2, spans.Count);
        Assert.AreEqual("新", spans[0].Attribute("Text")?.Value);
        Assert.AreEqual("标题", spans[1].Attribute("Text")?.Value);
    }

    [TestMethod(DisplayName = "流式合并：Rect 的 Fill 子元素不要求 Id")]
    public void Merge_PageFragmentWithRectFill_DoesNotRequireFillId()
    {
        // Arrange
        var merger = new SlideMlStreamingMerger();
        var context = new SlideMlPipelineContext();

        merger.AcceptFragment("<Page><Rect Id=\"bg\" Width=\"100\" Height=\"50\"/></Page>", context);

        // Act
        merger.AcceptFragment("<Page><Rect Id=\"bg\"><Fill><LinearGradient><Stop Offset=\"0\" Color=\"#000000\"/><Stop Offset=\"1\" Color=\"#FFFFFF\"/></LinearGradient></Fill></Rect></Page>", context);

        // Assert
        Assert.IsFalse(context.Errors.Any(e => e.Contains("元素缺少 Id") && (e.Contains("Fill") || e.Contains("LinearGradient") || e.Contains("Stop"))),
            $"Fill/LinearGradient/Stop 子元素不应要求 Id，实际错误: {string.Join("; ", context.Errors)}");

        var xml = merger.GetMergedXml();
        var doc = XDocument.Parse(xml);
        var rect = doc.Root!.Elements("Rect").Single();
        var fill = rect.Element("Fill");

        Assert.IsNotNull(fill);
        Assert.IsNotNull(fill.Element("LinearGradient"));
        Assert.AreEqual(2, fill.Element("LinearGradient")!.Elements("Stop").Count());
    }

    [TestMethod]
    public void DuplicateIdInSameFragment_DifferentTypes_Error()
    {
        // Arrange
        var merger = new SlideMlStreamingMerger();
        var context = new SlideMlPipelineContext();

        // Act — 同一片段内 Panel 和 Rect 都叫 dup（类型不同）
        merger.AcceptFragment("<Page/>", context);
        merger.AcceptFragment("<Page><Panel Id=\"dup\"><Rect Id=\"dup\"/></Panel></Page>", context);

        // Assert
        Assert.IsNotEmpty(context.Errors, "同片段内不同类型元素共用 Id 应产生错误");
    }

    [TestMethod]
    public void DuplicateIdInSameFragment_SameType_WarningOnly()
    {
        // Arrange
        var merger = new SlideMlStreamingMerger();
        var context = new SlideMlPipelineContext();

        // Act — 同一片段内两个 Rect 都叫 dup（类型相同）
        merger.AcceptFragment("<Page><Panel Id=\"p1\"><Rect Id=\"dup\"/><Rect Id=\"dup\"/></Panel></Page>", context);

        // Assert
        Assert.IsEmpty(context.Errors, "同类型重复 Id 不应产生错误");
        Assert.IsNotEmpty(context.Warnings, "同类型重复 Id 应产生警告");
    }

    [TestMethod]
    public void DuplicateId_CrossFragment_DifferentTypes_Error()
    {
        // Arrange
        var merger = new SlideMlStreamingMerger();
        var context = new SlideMlPipelineContext();

        // Act — 先放一个 Panel，再用 Rect 覆盖同一 Id
        merger.AcceptFragment("<Page><Panel Id=\"dup\" X=\"0\" Y=\"0\"/></Page>", context);
        merger.AcceptFragment("<Rect Id=\"dup\" Width=\"100\"/>", context);

        // Assert
        Assert.IsNotEmpty(context.Errors, "跨片段不同类型元素共用 Id 应产生错误");
    }

    [TestMethod]
    public void DuplicateId_CrossFragment_SameType_Merges()
    {
        // Arrange
        var merger = new SlideMlStreamingMerger();
        var context = new SlideMlPipelineContext();

        // Act — 先放一个 Rect，再用 Rect 覆盖同一 Id（类型相同，正常合并）
        merger.AcceptFragment("<Page><Rect Id=\"dup\" Width=\"100\" Height=\"50\" Fill=\"#FF0000\"/></Page>", context);
        merger.AcceptFragment("<Page><Rect Id=\"dup\" Width=\"200\"/></Page>", context);

        // Assert — MergeChildren 整体替换子元素，Width 来自新片段
        Assert.IsEmpty(context.Errors, "同类型跨片段 Id 重复不应产生错误");
        var xml = merger.GetMergedXml();
        StringAssert.Contains(xml, "Width=\"200\"", "新片段的 Width 应生效");
        StringAssert.Contains(xml, "Id=\"dup\"", "dup 元素应存在");
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

    [TestMethod]
    public void DanglingElement_MissingStyleId_Error()
    {
        // Arrange
        var merger = new SlideMlStreamingMerger();
        var context = new SlideMlPipelineContext();

        // Act — 顶层元素不在 Page 子树内，且没有 StyleId
        merger.AcceptFragment("<Rect Id=\"template\" Fill=\"#FF0000\"/>", context);

        // Assert
        Assert.IsTrue(context.Errors.Any(e => e.Contains("StyleId")), "悬空元素缺少 StyleId 应产生错误");
    }

    [TestMethod]
    public void StyleFrom_ReferencesStyleId_CopiesAttributes()
    {
        // Arrange
        var merger = new SlideMlStreamingMerger();
        var context = new SlideMlPipelineContext();

        // Act — 悬空模板带 StyleId，后续元素通过 StyleFrom 引用 StyleId
        merger.AcceptFragment("<Rect Id=\"template\" StyleId=\"card-style\" Fill=\"#FF0000\" Width=\"100\" Height=\"50\" CornerRadius=\"8\"/>", context);
        merger.AcceptFragment("<Page><Rect Id=\"card1\" StyleFrom=\"card-style\" X=\"10\" Y=\"20\"/></Page>", context);

        // Assert
        var xml = merger.GetMergedXml();
        var doc = XDocument.Parse(xml);
        var card1 = doc.Root!
            .Elements()
            .First(e => e.Attribute("Id")?.Value == "card1");

        Assert.AreEqual("#FF0000", card1.Attribute("Fill")?.Value, "应从 StyleId 源继承 Fill");
        Assert.AreEqual("100", card1.Attribute("Width")?.Value, "应从 StyleId 源继承 Width");
        Assert.AreEqual("50", card1.Attribute("Height")?.Value, "应从 StyleId 源继承 Height");
        Assert.AreEqual("8", card1.Attribute("CornerRadius")?.Value, "应从 StyleId 源继承 CornerRadius");
        Assert.AreEqual("10", card1.Attribute("X")?.Value, "应保留自身 X");
        Assert.AreEqual("20", card1.Attribute("Y")?.Value, "应保留自身 Y");
        Assert.IsNull(card1.Attribute("StyleFrom"), "StyleFrom 属性应被移除");
        Assert.IsNull(card1.Attribute("StyleId"), "不应从源元素复制 StyleId");
    }

    [TestMethod]
    public void StyleFrom_StyleIdNotFound_Error()
    {
        // Arrange
        var merger = new SlideMlStreamingMerger();
        var context = new SlideMlPipelineContext();

        // Act — StyleFrom 引用不存在的 StyleId
        merger.AcceptFragment("<Page/>", context);
        merger.AcceptFragment("<Page><Rect Id=\"card1\" StyleFrom=\"nonexistent-style\" X=\"0\"/></Page>", context);

        // Assert
        Assert.IsTrue(context.Errors.Any(e => e.Contains("nonexistent-style")), "应产生包含 nonexistent-style 的错误");
    }

    [TestMethod]
    public void StyleFrom_ReferencesIdInsteadOfStyleId_Error()
    {
        // Arrange
        var merger = new SlideMlStreamingMerger();
        var context = new SlideMlPipelineContext();

        // Act — 元素有 Id 但没有 StyleId，StyleFrom 引用该 Id 应失败
        merger.AcceptFragment("<Page><Rect Id=\"source\" Fill=\"#FF0000\" Width=\"100\"/></Page>", context);
        merger.AcceptFragment("<Page><Rect Id=\"target\" StyleFrom=\"source\" X=\"0\"/></Page>", context);

        // Assert — StyleFrom 查找 StyleId "source"，但该元素只有 Id 没有 StyleId
        Assert.IsTrue(context.Errors.Any(e => e.Contains("source")), "StyleFrom 引用 Id 而非 StyleId 应产生错误");
    }

    [TestMethod]
    public void StyleId_PageElement_CanBeReferenced()
    {
        // Arrange
        var merger = new SlideMlStreamingMerger();
        var context = new SlideMlPipelineContext();

        // Act — Page 子树内元素带 StyleId，被另一个元素通过 StyleFrom 引用
        merger.AcceptFragment("<Page><Rect Id=\"base\" StyleId=\"base-style\" Fill=\"#0000FF\" Width=\"200\" Height=\"100\"/></Page>", context);
        merger.AcceptFragment("<Page><Rect Id=\"derived\" StyleFrom=\"base-style\" Stroke=\"#FF0000\"/></Page>", context);

        // Assert
        var xml = merger.GetMergedXml();
        var doc = XDocument.Parse(xml);
        var derived = doc.Root!
            .Elements()
            .First(e => e.Attribute("Id")?.Value == "derived");

        Assert.AreEqual("#0000FF", derived.Attribute("Fill")?.Value, "应从 Page 内元素的 StyleId 继承 Fill");
        Assert.AreEqual("200", derived.Attribute("Width")?.Value, "应从 Page 内元素的 StyleId 继承 Width");
        Assert.AreEqual("100", derived.Attribute("Height")?.Value, "应从 Page 内元素的 StyleId 继承 Height");
        Assert.AreEqual("#FF0000", derived.Attribute("Stroke")?.Value, "应保留自身 Stroke");
    }

    [TestMethod]
    public void StyleId_Duplicate_Error()
    {
        // Arrange
        var merger = new SlideMlStreamingMerger();
        var context = new SlideMlPipelineContext();

        // Act — 两个元素声明了相同的 StyleId
        merger.AcceptFragment("<Page><Rect Id=\"a\" StyleId=\"dup-style\" Fill=\"#FF0000\"/></Page>", context);
        merger.AcceptFragment("<Page><Rect Id=\"b\" StyleId=\"dup-style\" Fill=\"#00FF00\"/></Page>", context);

        // Assert
        Assert.IsTrue(context.Errors.Any(e => e.Contains("dup-style")), "重复 StyleId 应产生错误");
    }

    [TestMethod]
    public void DanglingElement_WithStyleId_RegisteredInStyleIdIndex()
    {
        // Arrange
        var merger = new SlideMlStreamingMerger();
        var context = new SlideMlPipelineContext();

        // Act — 悬空元素带 StyleId，后续 Page 内元素通过 StyleFrom 引用
        merger.AcceptFragment("<Rect Id=\"tmpl\" StyleId=\"tmpl-style\" Fill=\"#CCCCCC\" Width=\"300\" Height=\"200\"/>", context);
        merger.AcceptFragment("<Page><Rect Id=\"card\" StyleFrom=\"tmpl-style\" X=\"50\" Y=\"50\"/></Page>", context);

        // Assert
        var xml = merger.GetMergedXml();
        var doc = XDocument.Parse(xml);
        Assert.AreEqual("Page", doc.Root!.Name.LocalName);
        var card = doc.Root!.Elements().First(e => e.Attribute("Id")?.Value == "card");
        Assert.AreEqual("#CCCCCC", card.Attribute("Fill")?.Value, "应从悬空模板的 StyleId 继承 Fill");
        Assert.AreEqual("300", card.Attribute("Width")?.Value, "应从悬空模板的 StyleId 继承 Width");
        Assert.AreEqual("200", card.Attribute("Height")?.Value, "应从悬空模板的 StyleId 继承 Height");
        Assert.IsNull(doc.Root!.Elements().FirstOrDefault(e => e.Attribute("Id")?.Value == "tmpl"), "悬空模板不应出现在 Page 子树中");
    }

    [TestMethod]
    public void StyleFrom_DoesNotCopyIdOrStyleId()
    {
        // Arrange
        var merger = new SlideMlStreamingMerger();
        var context = new SlideMlPipelineContext();

        // Act
        merger.AcceptFragment("<Rect Id=\"src\" StyleId=\"src-style\" Fill=\"#FF0000\" Width=\"100\"/>", context);
        merger.AcceptFragment("<Page><Rect Id=\"dst\" StyleFrom=\"src-style\" X=\"0\"/></Page>", context);

        // Assert
        var xml = merger.GetMergedXml();
        var doc = XDocument.Parse(xml);
        var dst = doc.Root!.Elements().First(e => e.Attribute("Id")?.Value == "dst");
        Assert.AreEqual("dst", dst.Attribute("Id")?.Value, "Id 应保持自身值");
        Assert.IsNull(dst.Attribute("StyleId"), "不应从源元素复制 StyleId");
        Assert.AreEqual("#FF0000", dst.Attribute("Fill")?.Value, "应继承 Fill");
    }

    [TestMethod(DisplayName = "回滚：出错片段后调用 RollbackLastVersion 恢复到合并前状态")]
    public void Rollback_ErrorFragment_RestoresPreviousState()
    {
        // Arrange
        var merger = new SlideMlStreamingMerger();
        var context = new SlideMlPipelineContext();

        // 先合并一个成功的 Page
        merger.AcceptFragment("<Page><Rect Id=\"r1\" Width=\"100\" Height=\"50\"/></Page>", context);
        var xmlBeforeError = merger.GetMergedXml();

        // Act — 合并一个出错的片段（跨片段不同类型 Id 冲突）
        merger.AcceptFragment("<Panel Id=\"r1\" X=\"0\"/>", context);

        // Assert — 出错后 DOM 已被污染
        Assert.IsNotEmpty(context.Errors, "应产生类型冲突错误");

        // 回滚到最后一个干净版本
        var rolledBack = merger.RollbackLastVersion();

        Assert.IsTrue(rolledBack, "回滚应成功");
        Assert.AreEqual(xmlBeforeError, merger.GetMergedXml(), "回滚后 XML 应恢复到出错前状态");
    }

    [TestMethod(DisplayName = "回滚：成功片段不产生版本快照，RollbackLastVersion 返回 false")]
    public void Rollback_NoErrorFragment_ReturnsFalse()
    {
        // Arrange
        var merger = new SlideMlStreamingMerger();
        var context = new SlideMlPipelineContext();

        merger.AcceptFragment("<Page><Rect Id=\"r1\" Width=\"100\"/></Page>", context);

        // Act
        var rolledBack = merger.RollbackLastVersion();

        // Assert — 成功的合并不产生快照
        Assert.IsFalse(rolledBack, "无错误时回滚应返回 false");
        Assert.Contains("r1", merger.GetMergedXml(), "内容不应变化");
    }

    [TestMethod(DisplayName = "回滚：出错后回滚，再出错可再次回滚")]
    public void Rollback_ErrorFragment_RollbackThenErrorAgain_CanRollbackAgain()
    {
        // Arrange
        var merger = new SlideMlStreamingMerger();
        var context = new SlideMlPipelineContext();

        // 第一个成功片段
        merger.AcceptFragment("<Page><Rect Id=\"r1\" Width=\"100\"/></Page>", context);
        var xmlAfterFirst = merger.GetMergedXml();

        // 第一个错误片段
        merger.AcceptFragment("<Panel Id=\"r1\"/>", context);

        // 第一次回滚
        var firstRollback = merger.RollbackLastVersion();
        Assert.IsTrue(firstRollback, "第一次回滚应成功");
        Assert.AreEqual(xmlAfterFirst, merger.GetMergedXml(), "回滚后应恢复到第一个成功片段后的状态");

        // 第二个错误片段（回滚后再次出错）
        context.Reset();
        merger.AcceptFragment("<Panel Id=\"r1\"/>", context);

        // Act — 第二次回滚
        var secondRollback = merger.RollbackLastVersion();

        // Assert
        Assert.IsTrue(secondRollback, "第二次回滚应成功");
        Assert.AreEqual(xmlAfterFirst, merger.GetMergedXml(), "第二次回滚后应仍恢复到已确认状态");
    }

    [TestMethod(DisplayName = "回滚：XML 格式错误的片段也产生快照并可回滚")]
    public void Rollback_XmlFormatError_CanRollback()
    {
        // Arrange
        var merger = new SlideMlStreamingMerger();
        var context = new SlideMlPipelineContext();

        merger.AcceptFragment("<Page><Rect Id=\"r1\" Width=\"100\"/></Page>", context);
        var xmlBeforeError = merger.GetMergedXml();

        // Act — XML 格式错误
        merger.AcceptFragment("<Panel Id=\"bad\"", context);

        Assert.IsNotEmpty(context.Errors, "应产生 XML 格式错误");

        var rolledBack = merger.RollbackLastVersion();

        // Assert
        Assert.IsTrue(rolledBack, "回滚应成功");
        Assert.AreEqual(xmlBeforeError, merger.GetMergedXml(), "回滚后应恢复到出错前状态");
    }

    [TestMethod(DisplayName = "回滚：Reset 清空版本栈")]
    public void Reset_ClearsVersionStack()
    {
        // Arrange
        var merger = new SlideMlStreamingMerger();
        var context = new SlideMlPipelineContext();

        merger.AcceptFragment("<Page/>", context);
        merger.AcceptFragment("<Panel Id=\"r1\"/>", context); // 类型冲突错误

        // Act
        merger.Reset();

        // Assert — Reset 后回滚应无效
        var rolledBack = merger.RollbackLastVersion();
        Assert.IsFalse(rolledBack, "Reset 后版本栈应为空，回滚应返回 false");
    }

    [TestMethod(DisplayName = "回滚：回滚后可继续正常合并")]
    public void Rollback_ThenContinueMerge_WorksCorrectly()
    {
        // Arrange
        var merger = new SlideMlStreamingMerger();
        var context = new SlideMlPipelineContext();

        merger.AcceptFragment("<Page><Rect Id=\"r1\" Width=\"100\"/></Page>", context);
        merger.AcceptFragment("<Panel Id=\"r1\"/>", context); // 类型冲突错误
        merger.RollbackLastVersion();

        // Act — 回滚后继续合并一个正确的片段
        context.Reset();
        merger.AcceptFragment("<Page><Rect Id=\"r2\" Width=\"200\"/></Page>", context);

        // Assert
        Assert.IsEmpty(context.Errors, "回滚后继续合并不应产生错误");
        var xml = merger.GetMergedXml();
        Assert.Contains("r1", xml, "r1 应保留");
        Assert.Contains("r2", xml, "r2 应存在");
    }

    [TestMethod(DisplayName = "回滚：回滚后 Id 索引正确，可以再次合并同 Id 元素")]
    public void Rollback_IdIndexRestored_CanMergeSameIdAgain()
    {
        // Arrange
        var merger = new SlideMlStreamingMerger();
        var context = new SlideMlPipelineContext();

        merger.AcceptFragment("<Page><Rect Id=\"r1\" Width=\"100\" Height=\"50\"/></Page>", context);
        merger.AcceptFragment("<Panel Id=\"r1\"/>", context); // 类型冲突错误
        merger.RollbackLastVersion();

        // Act — 回滚后用同类型元素覆盖 r1
        context.Reset();
        merger.AcceptFragment("<Rect Id=\"r1\" Width=\"200\"/>", context);

        // Assert
        Assert.IsEmpty(context.Errors, "回滚后同类型合并不应产生错误");
        var xml = merger.GetMergedXml();
        Assert.Contains("Width=\"200\"", xml, "Width 应被更新为 200");
    }

    // ───────── 边界行为：首个片段即出错 ─────────

    [TestMethod(DisplayName = "首个片段即出错：回滚后 GetMergedXml 返回空字符串")]
    public void FirstFragmentError_RollsBackToEmptyString()
    {
        // Arrange
        var merger = new SlideMlStreamingMerger();
        var context = new SlideMlPipelineContext();

        // Act — 首个片段就是错误片段（未知根元素）
        merger.AcceptFragment("<UnknownElement Id=\"x\"/>", context);

        // Assert — 出错后有 working 副本，GetMergedXml 返回 working 的 XML（可能为空）
        Assert.IsNotEmpty(context.Errors, "应产生未知元素错误");

        var rolledBack = merger.RollbackLastVersion();
        Assert.IsTrue(rolledBack, "应成功回滚");

        // 回滚后 committed 仍为初始空状态
        var xml = merger.GetMergedXml();
        Assert.IsTrue(string.IsNullOrEmpty(xml), "回滚后 DOM 应为空字符串");
    }

    [TestMethod(DisplayName = "首个片段即 XML 格式错误：回滚后 GetMergedXml 返回空字符串")]
    public void FirstFragmentXmlFormatError_RollsBackToEmptyString()
    {
        // Arrange
        var merger = new SlideMlStreamingMerger();
        var context = new SlideMlPipelineContext();

        // Act — 首个片段是格式错误的 XML
        merger.AcceptFragment("<Panel Id=\"bad\"", context);

        // Assert
        Assert.IsNotEmpty(context.Errors, "应产生 XML 格式错误");

        var rolledBack = merger.RollbackLastVersion();
        Assert.IsTrue(rolledBack, "应成功回滚");
        Assert.IsTrue(string.IsNullOrEmpty(merger.GetMergedXml()), "回滚后 DOM 应为空字符串");
    }

    [TestMethod(DisplayName = "首个片段即出错：回滚后后续正确片段可正常合并")]
    public void FirstFragmentError_RollbackThenValidFragment_MergesSuccessfully()
    {
        // Arrange
        var merger = new SlideMlStreamingMerger();
        var context = new SlideMlPipelineContext();

        // 首个片段出错
        merger.AcceptFragment("<UnknownElement Id=\"bad\"/>", context);
        merger.RollbackLastVersion();

        // Act — 回滚后继续合并正确片段
        context.Reset();
        merger.AcceptFragment("<Page><Rect Id=\"r1\" Width=\"100\" Height=\"50\"/></Page>", context);

        // Assert — 从空状态成功合并
        Assert.IsEmpty(context.Errors, "后续正确片段不应出错");
        var xml = merger.GetMergedXml();
        var doc = XDocument.Parse(xml);
        Assert.AreEqual("Page", doc.Root!.Name.LocalName, "应有 Page 根元素");
        Assert.Contains("r1", xml, "r1 应存在");
    }

    [TestMethod(DisplayName = "首个片段即出错：回滚后 Id 索引为空，同 Id 可正常注册")]
    public void FirstFragmentError_Rollback_IdIndexEmpty_CanRegisterSameId()
    {
        // Arrange
        var merger = new SlideMlStreamingMerger();
        var context = new SlideMlPipelineContext();

        // 首个片段出错（带 Id="x" 的未知元素）
        merger.AcceptFragment("<UnknownElement Id=\"x\"/>", context);
        merger.RollbackLastVersion();

        // Act — 回滚后 Id 索引应不包含 "x"，可以用同 Id 注册
        context.Reset();
        merger.AcceptFragment("<Page><Rect Id=\"x\" Width=\"100\"/></Page>", context);

        // Assert
        Assert.IsEmpty(context.Errors, "回滚后同 Id 应可正常注册");
        var xml = merger.GetMergedXml();
        Assert.Contains("Id=\"x\"", xml, "Id=\"x\" 应存在");
    }

    [TestMethod(DisplayName = "首个片段即出错：连续两次出错后回滚仍为空")]
    public void FirstFragmentError_ConsecutiveErrors_BothRollBackToEmpty()
    {
        // Arrange
        var merger = new SlideMlStreamingMerger();
        var context = new SlideMlPipelineContext();

        // 第一次出错
        merger.AcceptFragment("<UnknownElement Id=\"bad1\"/>", context);
        var firstRollback = merger.RollbackLastVersion();
        Assert.IsTrue(firstRollback, "第一次回滚应成功");
        Assert.IsTrue(string.IsNullOrEmpty(merger.GetMergedXml()), "第一次回滚后 DOM 应为空");

        // 第二次仍然出错（从空 committed 状态克隆）
        context.Reset();
        merger.AcceptFragment("<AnotherBadElement Id=\"bad2\"/>", context);
        var secondRollback = merger.RollbackLastVersion();
        Assert.IsTrue(secondRollback, "第二次回滚应成功");
        Assert.IsTrue(string.IsNullOrEmpty(merger.GetMergedXml()), "第二次回滚后 DOM 应仍为空");
    }

    [TestMethod(DisplayName = "首个片段即出错：回滚后无 working 副本，再次 Rollback 返回 false")]
    public void FirstFragmentError_RollbackThenRollbackAgain_ReturnsFalse()
    {
        // Arrange
        var merger = new SlideMlStreamingMerger();
        var context = new SlideMlPipelineContext();

        merger.AcceptFragment("<UnknownElement Id=\"bad\"/>", context);
        merger.RollbackLastVersion();

        // Act — 再次回滚（无 working 副本）
        var secondRollback = merger.RollbackLastVersion();

        // Assert
        Assert.IsFalse(secondRollback, "无 working 副本时回滚应返回 false");
    }

    [TestMethod(DisplayName = "首个片段即出错：回滚后 Reset 再合并正确片段正常工作")]
    public void FirstFragmentError_RollbackThenReset_ThenValidFragment()
    {
        // Arrange
        var merger = new SlideMlStreamingMerger();
        var context = new SlideMlPipelineContext();

        merger.AcceptFragment("<UnknownElement Id=\"bad\"/>", context);
        merger.RollbackLastVersion();
        merger.Reset();

        // Act
        context.Reset();
        merger.AcceptFragment("<Page><Rect Id=\"r1\" Width=\"100\"/></Page>", context);

        // Assert
        Assert.IsEmpty(context.Errors, "Reset 后正确片段不应出错");
        Assert.Contains("r1", merger.GetMergedXml(), "r1 应存在");
    }

    [TestMethod(DisplayName = "首个片段即出错：出错后未回滚时 GetMergedXml 返回 working 状态（可能为空）")]
    public void FirstFragmentError_WithoutRollback_GetMergedXmlReturnsWorkingState()
    {
        // Arrange
        var merger = new SlideMlStreamingMerger();
        var context = new SlideMlPipelineContext();

        // Act — 首个片段出错但不回滚
        merger.AcceptFragment("<UnknownElement Id=\"bad\"/>", context);

        // Assert — GetMergedXml 返回 working（从空 committed 克隆，Document 仍为 null）
        Assert.IsNotEmpty(context.Errors, "应产生错误");
        var xml = merger.GetMergedXml();
        // working 是从空 committed 克隆的，未做任何修改（出错前没走到文档操作），所以也为空
        Assert.IsTrue(string.IsNullOrEmpty(xml),
            "首个片段出错时 working 是从空状态克隆，Document 仍为 null，GetMergedXml 应返回空");
    }

    [TestMethod(DisplayName = "首个片段即出错：Page 内同片段 Id 类型冲突导致整个片段回滚，DOM 为空")]
    public void FirstFragmentError_PageChildDuplicateIdTypeConflict_RollsBackEntireFragment()
    {
        // Arrange
        var merger = new SlideMlStreamingMerger();
        var context = new SlideMlPipelineContext();

        // Act — 首个片段 Page 内 Panel 和 Rect 共用 Id="dup"（类型不同）
        // AcceptFragment 的 fragmentIds 检查在 ProcessPage 之前执行，检测到类型冲突
        // 此时 _working 已从空 committed 克隆，但 ProcessPage 尚未执行（错误在 fragmentIds 检查阶段触发）
        merger.AcceptFragment("<Page><Panel Id=\"dup\"><Rect Id=\"dup\"/></Panel></Page>", context);

        // Assert — 应产生同片段内 Id 类型冲突错误
        Assert.IsNotEmpty(context.Errors, "应产生同片段内 Id 类型冲突错误");

        // 出错后 working 是从空 committed 克隆的，Document 仍为 null（ProcessPage 未执行）
        Assert.IsTrue(string.IsNullOrEmpty(merger.GetMergedXml()),
            "首个片段出错时 working 从空状态克隆，Document 仍为 null，GetMergedXml 应返回空");

        // 回滚后 committed 仍为空（初始状态）
        var rolledBack = merger.RollbackLastVersion();
        Assert.IsTrue(rolledBack, "应成功回滚");
        Assert.IsTrue(string.IsNullOrEmpty(merger.GetMergedXml()),
            "回滚后 DOM 应为空（整个 Page 片段被丢弃）");
    }
}
