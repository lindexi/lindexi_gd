using PptxGenerator.Models;
using PptxGenerator.Models.SlideDocuments;

namespace PptxGenerator.Tests.Models;

[TestClass]
public sealed class SlideMlXmlUtilitiesFormatRenderedXmlTests
{
    private static SlideMlPipelineContext CreateContext() => new();

    [TestMethod]
    public void FormatRenderedXml_Page_BackfillsCanvasSize()
    {
        var xml = "<Page Background=\"#FFF\"><Rect Id=\"r1\" Width=\"100\" Height=\"50\"/></Page>";
        var page = new SlideMlPage
        {
            Children =
            {
                new SlideMlRectElement { Id = "r1", Width = 100, Height = 50, ActualWidth = 100, ActualHeight = 50 },
            },
        };
        var context = CreateContext();

        var result = SlideMlXmlUtilities.FormatRenderedXml(xml, page, context);

        Assert.IsTrue(result.Contains("ActualWidth=\"1280\""));
        Assert.IsTrue(result.Contains("ActualHeight=\"720\""));
    }

    [TestMethod]
    public void FormatRenderedXml_AllElementTypes_Backfilled()
    {
        var xml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <Page>
              <Panel Id="p1">
                <Rect Id="r1" Width="100" Height="50"/>
                <TextElement Id="t1" Text="hello"/>
                <Image Id="img1" Source="test.png"/>
              </Panel>
            </Page>
            """;
        var page = new SlideMlPage
        {
            Children =
            {
                new SlideMlPanelElement
                {
                    Id = "p1",
                    ActualWidth = 200,
                    ActualHeight = 150,
                    Children =
                    {
                        new SlideMlRectElement { Id = "r1", ActualWidth = 100, ActualHeight = 50 },
                        new SlideMlTextElement { Id = "t1", ActualWidth = 80, ActualHeight = 20 },
                        new SlideMlImageElement { Id = "img1", ActualWidth = 60, ActualHeight = 40 },
                    },
                },
            },
        };
        var context = CreateContext();

        var result = SlideMlXmlUtilities.FormatRenderedXml(xml, page, context);

        Assert.IsTrue(result.Contains("Id=\"p1\"") && result.Contains("ActualWidth=\"200\""));
        Assert.IsTrue(result.Contains("Id=\"r1\"") && result.Contains("ActualWidth=\"100\""));
        Assert.IsTrue(result.Contains("Id=\"t1\"") && result.Contains("ActualWidth=\"80\""));
        Assert.IsTrue(result.Contains("Id=\"img1\"") && result.Contains("ActualWidth=\"60\""));
    }

    [TestMethod]
    public void FormatRenderedXml_ActualLineCount_OnlyOnText()
    {
        var xml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <Page>
              <TextElement Id="t1" Text="hello"/>
              <Rect Id="r1" Width="100" Height="50"/>
            </Page>
            """;
        var page = new SlideMlPage
        {
            Children =
            {
                new SlideMlTextElement { Id = "t1", ActualWidth = 80, ActualHeight = 60, ActualLineCount = 3 },
                new SlideMlRectElement { Id = "r1", ActualWidth = 100, ActualHeight = 50 },
            },
        };
        var context = CreateContext();

        var result = SlideMlXmlUtilities.FormatRenderedXml(xml, page, context);

        // TextElement 应有 ActualLineCount
        var t1Start = result.IndexOf("Id=\"t1\"");
        Assert.IsTrue(t1Start >= 0);
        var t1Segment = result.Substring(t1Start, result.IndexOf("/>", t1Start) - t1Start + 2);
        Assert.IsTrue(t1Segment.Contains("ActualLineCount=\"3\""));

        // Rect 不应有 ActualLineCount
        var r1Start = result.IndexOf("Id=\"r1\"");
        Assert.IsTrue(r1Start >= 0);
        var r1Segment = result.Substring(r1Start, result.IndexOf("/>", r1Start) - r1Start + 2);
        Assert.IsFalse(r1Segment.Contains("ActualLineCount"));
    }

    [TestMethod]
    public void FormatRenderedXml_NestedElements_AllBackfilled()
    {
        var xml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <Page>
              <Panel Id="outer">
                <Panel Id="inner">
                  <Rect Id="leaf"/>
                </Panel>
              </Panel>
            </Page>
            """;
        var page = new SlideMlPage
        {
            Children =
            {
                new SlideMlPanelElement
                {
                    Id = "outer",
                    ActualWidth = 300,
                    ActualHeight = 200,
                    Children =
                    {
                        new SlideMlPanelElement
                        {
                            Id = "inner",
                            ActualWidth = 150,
                            ActualHeight = 100,
                            Children =
                            {
                                new SlideMlRectElement { Id = "leaf", ActualWidth = 50, ActualHeight = 30 },
                            },
                        },
                    },
                },
            },
        };
        var context = CreateContext();

        var result = SlideMlXmlUtilities.FormatRenderedXml(xml, page, context);

        Assert.IsTrue(result.Contains("Id=\"outer\"") && result.Contains("ActualWidth=\"300\""));
        Assert.IsTrue(result.Contains("Id=\"inner\"") && result.Contains("ActualWidth=\"150\""));
        Assert.IsTrue(result.Contains("Id=\"leaf\"") && result.Contains("ActualWidth=\"50\""));
    }

    [TestMethod]
    public void FormatRenderedXml_IdNotFound_Skipped()
    {
        var xml = "<Page><Rect Id=\"r1\"/></Page>";
        var page = new SlideMlPage();
        var context = CreateContext();

        var result = SlideMlXmlUtilities.FormatRenderedXml(xml, page, context);

        // Page 仍然有 ActualWidth/ActualHeight
        Assert.IsTrue(result.Contains("ActualWidth=\"1280\""));
        // r1 不应有 ActualWidth（因为 FindMetrics 找不到）
        var r1Start = result.IndexOf("Id=\"r1\"");
        Assert.IsTrue(r1Start >= 0);
        var r1Segment = result.Substring(r1Start, result.IndexOf("/>", r1Start) - r1Start + 2);
        Assert.IsFalse(r1Segment.Contains("ActualWidth"));
    }

    [TestMethod]
    public void FormatRenderedXml_NullXml_ThrowsArgumentNullException()
    {
        var page = new SlideMlPage();
        var context = CreateContext();

        Assert.ThrowsExactly<ArgumentNullException>(() => SlideMlXmlUtilities.FormatRenderedXml(null!, page, context));
    }

    [TestMethod]
    public void FormatRenderedXml_NullPage_ThrowsArgumentNullException()
    {
        var xml = "<Page></Page>";
        var context = CreateContext();

        Assert.ThrowsExactly<ArgumentNullException>(() => SlideMlXmlUtilities.FormatRenderedXml(xml, null!, context));
    }

    [TestMethod]
    public void FormatRenderedXml_OriginalAttributes_Preserved()
    {
        var xml = "<Page><Rect Id=\"r1\" X=\"10\" Y=\"20\" Width=\"100\" Height=\"50\" Fill=\"#FF0000\" CornerRadius=\"8\"/></Page>";
        var page = new SlideMlPage
        {
            Children =
            {
                new SlideMlRectElement { Id = "r1", ActualWidth = 100, ActualHeight = 50 },
            },
        };
        var context = CreateContext();

        var result = SlideMlXmlUtilities.FormatRenderedXml(xml, page, context);

        Assert.IsTrue(result.Contains("X=\"10\""));
        Assert.IsTrue(result.Contains("Y=\"20\""));
        Assert.IsTrue(result.Contains("Width=\"100\""));
        Assert.IsTrue(result.Contains("Height=\"50\""));
        Assert.IsTrue(result.Contains("Fill=\"#FF0000\""));
        Assert.IsTrue(result.Contains("CornerRadius=\"8\""));
        Assert.IsTrue(result.Contains("ActualWidth=\"100\""));
        Assert.IsTrue(result.Contains("ActualHeight=\"50\""));
    }
}
