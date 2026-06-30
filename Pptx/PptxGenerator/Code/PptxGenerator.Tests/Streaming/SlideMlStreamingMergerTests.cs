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

    [TestMethod(DisplayName = "回滚：连续多个错误片段可逐个回滚")]
    public void Rollback_MultipleErrorFragments_RollsBackOneByOne()
    {
        // Arrange
        var merger = new SlideMlStreamingMerger();
        var context = new SlideMlPipelineContext();

        // 第一个成功片段
        merger.AcceptFragment("<Page><Rect Id=\"r1\" Width=\"100\"/></Page>", context);
        var xmlAfterFirst = merger.GetMergedXml();

        // 第一个错误片段
        merger.AcceptFragment("<Panel Id=\"r1\"/>", context);

        // 第二个成功片段（基于被污染的状态）
        context.Reset();
        merger.AcceptFragment("<Page><Rect Id=\"r2\" Width=\"200\"/></Page>", context);
        var xmlAfterSecond = merger.GetMergedXml();

        // Act — 第一次回滚：恢复到第二个片段合并前（即第一个错误片段后的污染状态）
        var firstRollback = merger.RollbackLastVersion();

        // Assert
        Assert.IsTrue(firstRollback, "第一次回滚应成功");
        Assert.AreEqual(xmlAfterFirst, merger.GetMergedXml(), "第一次回滚应恢复到第一个成功片段后的状态");
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
}
