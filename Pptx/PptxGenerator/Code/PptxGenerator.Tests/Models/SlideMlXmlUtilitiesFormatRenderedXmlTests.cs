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
                new SlideMlRectElement { Id = "r1", Width = 100, Height = 50, MeasuredWidth = 100, MeasuredHeight = 50, LayoutBounds = new SlideMlRect(0, 0, 100, 50) },
            },
        };
        var context = CreateContext();

        var result = SlideMlXmlUtilities.FormatRenderedXml(xml, page, context);

        Assert.Contains("RenderSize=\"1280x720\"", result);
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
                    MeasuredWidth = 200,
                    MeasuredHeight = 150,
                    LayoutBounds = new SlideMlRect(0, 0, 200, 150),
                    Children =
                    {
                        new SlideMlRectElement { Id = "r1", MeasuredWidth = 100, MeasuredHeight = 50, LayoutBounds = new SlideMlRect(0, 0, 100, 50) },
                        new SlideMlTextElement { Id = "t1", MeasuredWidth = 80, MeasuredHeight = 20, LayoutBounds = new SlideMlRect(0, 0, 80, 20) },
                        new SlideMlImageElement { Id = "img1", MeasuredWidth = 60, MeasuredHeight = 40, LayoutBounds = new SlideMlRect(0, 0, 60, 40) },
                    },
                },
            },
        };
        var context = CreateContext();

        var result = SlideMlXmlUtilities.FormatRenderedXml(xml, page, context);

        Assert.Contains("Id=\"p1\"", result);
        Assert.Contains("RenderSize=\"200x150\"", result);
        Assert.Contains("Id=\"r1\"", result);
        Assert.Contains("RenderSize=\"100x50\"", result);
        Assert.Contains("Id=\"t1\"", result);
        Assert.Contains("RenderSize=\"80x20\"", result);
        Assert.Contains("Id=\"img1\"", result);
        Assert.Contains("RenderSize=\"60x40\"", result);
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
                new SlideMlTextElement { Id = "t1", MeasuredWidth = 80, MeasuredHeight = 60, LayoutBounds = new SlideMlRect(0, 0, 80, 60), ActualLineCount = 3 },
                new SlideMlRectElement { Id = "r1", MeasuredWidth = 100, MeasuredHeight = 50, LayoutBounds = new SlideMlRect(0, 0, 100, 50) },
            },
        };
        var context = CreateContext();

        var result = SlideMlXmlUtilities.FormatRenderedXml(xml, page, context);

        var t1Start = result.IndexOf("Id=\"t1\"");
        Assert.IsGreaterThanOrEqualTo(0, t1Start);
        var t1Segment = result.Substring(t1Start, result.IndexOf("/>", t1Start) - t1Start + 2);
        Assert.Contains("ActualLineCount=\"3\"", t1Segment);

        var r1Start = result.IndexOf("Id=\"r1\"");
        Assert.IsGreaterThanOrEqualTo(0, r1Start);
        var r1Segment = result.Substring(r1Start, result.IndexOf("/>", r1Start) - r1Start + 2);
        Assert.DoesNotContain("ActualLineCount", r1Segment);
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
                    MeasuredWidth = 300,
                    MeasuredHeight = 200,
                    LayoutBounds = new SlideMlRect(0, 0, 300, 200),
                    Children =
                    {
                        new SlideMlPanelElement
                        {
                            Id = "inner",
                            MeasuredWidth = 150,
                            MeasuredHeight = 100,
                            LayoutBounds = new SlideMlRect(0, 0, 150, 100),
                            Children =
                            {
                                new SlideMlRectElement { Id = "leaf", MeasuredWidth = 50, MeasuredHeight = 30, LayoutBounds = new SlideMlRect(0, 0, 50, 30) },
                            },
                        },
                    },
                },
            },
        };
        var context = CreateContext();

        var result = SlideMlXmlUtilities.FormatRenderedXml(xml, page, context);

        Assert.Contains("Id=\"outer\"", result);
        Assert.Contains("RenderSize=\"300x200\"", result);
        Assert.Contains("Id=\"inner\"", result);
        Assert.Contains("RenderSize=\"150x100\"", result);
        Assert.Contains("Id=\"leaf\"", result);
        Assert.Contains("RenderSize=\"50x30\"", result);
    }

    [TestMethod]
    public void FormatRenderedXml_IdNotFound_Skipped()
    {
        var xml = "<Page><Rect Id=\"r1\"/></Page>";
        var page = new SlideMlPage();
        var context = CreateContext();

        var result = SlideMlXmlUtilities.FormatRenderedXml(xml, page, context);

        Assert.Contains("RenderSize=\"1280x720\"", result);
        var r1Start = result.IndexOf("Id=\"r1\"");
        Assert.IsGreaterThanOrEqualTo(0, r1Start);
        var r1Segment = result.Substring(r1Start, result.IndexOf("/>", r1Start) - r1Start + 2);
        Assert.DoesNotContain("RenderSize", r1Segment);
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
                new SlideMlRectElement { Id = "r1", MeasuredWidth = 100, MeasuredHeight = 50, LayoutBounds = new SlideMlRect(0, 0, 100, 50) },
            },
        };
        var context = CreateContext();

        var result = SlideMlXmlUtilities.FormatRenderedXml(xml, page, context);

        Assert.Contains("X=\"10\"", result);
        Assert.Contains("Y=\"20\"", result);
        Assert.Contains("Width=\"100\"", result);
        Assert.Contains("Height=\"50\"", result);
        Assert.Contains("Fill=\"#FF0000\"", result);
        Assert.Contains("CornerRadius=\"8\"", result);
        Assert.Contains("RenderSize=\"100x50\"", result);
    }
}