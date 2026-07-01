using System.Collections.Generic;
using System.Linq;
using PptxGenerator.Models;
using PptxGenerator.Models.SlideDocuments;
using PptxGenerator.Rendering;

namespace PptxGenerator.Tests;

[TestClass]
public sealed class SlideMlLayoutEngineTests
{
    private SlideMlLayoutEngine _engine = null!;
    private SlideMlPipelineContext _context = null!;

    [TestInitialize]
    public void Setup()
    {
        _engine = new SlideMlLayoutEngine();
        _context = new SlideMlPipelineContext();
    }

    [TestMethod]
    public void HorizontalLayout_ChildrenPlacedSequentially()
    {
        var panel = new SlideMlPanelElement
        {
            Id = "panel1",
            Layout = SlideMlLayoutDirection.Horizontal,
            Gap = 8,
            Children =
            {
                new SlideMlRectElement { Id = "r1", Width = 100, Height = 50 },
                new SlideMlRectElement { Id = "r2", Width = 200, Height = 50 },
                new SlideMlRectElement { Id = "r3", Width = 150, Height = 50 },
            },
        };

        var page = CreatePage(panel);
        _engine.PreLayout(page, _context);

        Assert.IsEmpty(_context.Errors, "不应有错误");

        var r1 = (SlideMlRectElement)panel.Children[0];
        var r2 = (SlideMlRectElement)panel.Children[1];
        var r3 = (SlideMlRectElement)panel.Children[2];

        Assert.AreEqual(0, r1.LayoutBounds.X, 0.01, "r1 X");
        Assert.AreEqual(100 + 8, r2.LayoutBounds.X, 0.01, "r2 X");
        Assert.AreEqual(100 + 8 + 200 + 8, r3.LayoutBounds.X, 0.01, "r3 X");

        Assert.AreEqual(100 + 8 + 200 + 8 + 150, panel.ActualWidth, 0.01, "panel width");
        Assert.AreEqual(50, panel.ActualHeight, 0.01, "panel height");
    }

    [TestMethod]
    public void VerticalLayout_ChildrenPlacedSequentially()
    {
        var panel = new SlideMlPanelElement
        {
            Id = "panel1",
            Layout = SlideMlLayoutDirection.Vertical,
            Gap = 12,
            Children =
            {
                new SlideMlRectElement { Id = "r1", Width = 200, Height = 40 },
                new SlideMlRectElement { Id = "r2", Width = 200, Height = 60 },
            },
        };

        var page = CreatePage(panel);
        _engine.PreLayout(page, _context);

        var r1 = (SlideMlRectElement)panel.Children[0];
        var r2 = (SlideMlRectElement)panel.Children[1];

        Assert.AreEqual(0, r1.LayoutBounds.Y, 0.01, "r1 Y");
        Assert.AreEqual(40 + 12, r2.LayoutBounds.Y, 0.01, "r2 Y");
        Assert.AreEqual(40 + 12 + 60, panel.ActualHeight, 0.01, "panel height");
    }

    [TestMethod]
    public void HorizontalLayout_MarginAffectsSpacing()
    {
        var panel = new SlideMlPanelElement
        {
            Id = "panel1",
            Layout = SlideMlLayoutDirection.Horizontal,
            Gap = 4,
            Children =
            {
                new SlideMlRectElement { Id = "r1", Width = 100, Height = 50, Margin = new SlideMlThickness { Right = 20 } },
                new SlideMlRectElement { Id = "r2", Width = 100, Height = 50, Margin = new SlideMlThickness { Left = 10 } },
            },
        };

        var page = CreatePage(panel);
        _engine.PreLayout(page, _context);

        var r1 = (SlideMlRectElement)panel.Children[0];
        var r2 = (SlideMlRectElement)panel.Children[1];

        Assert.AreEqual(0, r1.LayoutBounds.X, 0.01, "r1 X");
        Assert.AreEqual(130, r2.LayoutBounds.X, 0.01, "r2 X with margin");
        Assert.AreEqual(230, panel.ActualWidth, 0.01, "panel width");
    }

    [TestMethod]
    public void HorizontalLayout_CrossAxisAlignment_Respected()
    {
        var panel = new SlideMlPanelElement
        {
            Id = "panel1",
            Layout = SlideMlLayoutDirection.Horizontal,
            Gap = 8,
            Height = 200,
            Children =
            {
                new SlideMlRectElement { Id = "r1", Width = 100, Height = 50, VerticalAlignment = SlideMlVerticalAlignment.Center },
                new SlideMlRectElement { Id = "r2", Width = 100, Height = 50, VerticalAlignment = SlideMlVerticalAlignment.Bottom },
            },
        };

        var page = CreatePage(panel);
        _engine.PreLayout(page, _context);

        var r1 = (SlideMlRectElement)panel.Children[0];
        var r2 = (SlideMlRectElement)panel.Children[1];

        Assert.AreEqual(75, r1.LayoutBounds.Y, 0.01, "r1 center Y");
        Assert.AreEqual(150, r2.LayoutBounds.Y, 0.01, "r2 bottom Y");
    }

    [TestMethod]
    public void FlowLayout_EmptyChildren_DoesNotCrash()
    {
        var panel = new SlideMlPanelElement
        {
            Id = "panel1",
            Layout = SlideMlLayoutDirection.Horizontal,
            Gap = 8,
        };

        var page = CreatePage(panel);
        _engine.PreLayout(page, _context);

        Assert.AreEqual(0, panel.ActualWidth, 0.01, "empty panel width");
        Assert.AreEqual(0, panel.ActualHeight, 0.01, "empty panel height");
    }

    [TestMethod]
    public void AbsoluteLayout_BehaviorUnchanged()
    {
        var panel = new SlideMlPanelElement
        {
            Id = "panel1",
            Layout = SlideMlLayoutDirection.Absolute,
            Children =
            {
                new SlideMlRectElement { Id = "r1", X = 50, Y = 30, Width = 100, Height = 50 },
                new SlideMlRectElement { Id = "r2", X = 200, Y = 100, Width = 150, Height = 80 },
            },
        };

        var page = CreatePage(panel);
        _engine.PreLayout(page, _context);

        var r1 = (SlideMlRectElement)panel.Children[0];
        var r2 = (SlideMlRectElement)panel.Children[1];

        Assert.AreEqual(50, r1.LayoutBounds.X, 0.01, "r1 X");
        Assert.AreEqual(30, r1.LayoutBounds.Y, 0.01, "r1 Y");
        Assert.AreEqual(200, r2.LayoutBounds.X, 0.01, "r2 X");
        Assert.AreEqual(100, r2.LayoutBounds.Y, 0.01, "r2 Y");
    }

    [TestMethod]
    public void HorizontalLayout_WithPadding_OffsetsContent()
    {
        var panel = new SlideMlPanelElement
        {
            Id = "panel1",
            Layout = SlideMlLayoutDirection.Horizontal,
            Gap = 8,
            Padding = new SlideMlThickness { Left = 16, Top = 16, Right = 16, Bottom = 16 },
            Children =
            {
                new SlideMlRectElement { Id = "r1", Width = 100, Height = 50 },
            },
        };

        var page = CreatePage(panel);
        _engine.PreLayout(page, _context);

        var r1 = (SlideMlRectElement)panel.Children[0];

        Assert.AreEqual(16, r1.LayoutBounds.X, 0.01, "r1 X with padding");
        Assert.AreEqual(16, r1.LayoutBounds.Y, 0.01, "r1 Y with padding");
        Assert.AreEqual(132, panel.ActualWidth, 0.01, "panel width with padding");
    }

    [TestMethod]
    public void NestedFlowPanels_LayoutCorrectly()
    {
        var innerPanel = new SlideMlPanelElement
        {
            Id = "inner",
            Layout = SlideMlLayoutDirection.Horizontal,
            Gap = 4,
            Children =
            {
                new SlideMlRectElement { Id = "ir1", Width = 50, Height = 30 },
                new SlideMlRectElement { Id = "ir2", Width = 50, Height = 30 },
            },
        };

        var outerPanel = new SlideMlPanelElement
        {
            Id = "outer",
            Layout = SlideMlLayoutDirection.Vertical,
            Gap = 10,
            Children =
            {
                new SlideMlRectElement { Id = "or1", Width = 200, Height = 40 },
                innerPanel,
            },
        };

        var page = CreatePage(outerPanel);
        _engine.PreLayout(page, _context);

        var or1 = (SlideMlRectElement)outerPanel.Children[0];

        Assert.AreEqual(0, or1.LayoutBounds.Y, 0.01, "or1 Y");
        Assert.AreEqual(40 + 10, innerPanel.LayoutBounds.Y, 0.01, "inner panel Y");

        var ir1 = (SlideMlRectElement)innerPanel.Children[0];
        var ir2 = (SlideMlRectElement)innerPanel.Children[1];
        Assert.AreEqual(0, ir1.LayoutBounds.X, 0.01, "ir1 X");
        Assert.AreEqual(50 + 4, ir2.LayoutBounds.X, 0.01, "ir2 X");
    }

    [TestMethod]
    public void FlowLayout_Overflow_GeneratesWarning()
    {
        var panel = new SlideMlPanelElement
        {
            Id = "panel1",
            Layout = SlideMlLayoutDirection.Horizontal,
            Gap = 8,
            Width = 150,
            Children =
            {
                new SlideMlRectElement { Id = "r1", Width = 100, Height = 50 },
                new SlideMlRectElement { Id = "r2", Width = 100, Height = 50 },
            },
        };

        var page = CreatePage(panel);
        _engine.PreLayout(page, _context);

        Assert.IsNotEmpty(_context.Warnings, "应有溢出警告");
        Assert.IsTrue(_context.Warnings.Any(w => w.Contains("超出")), "警告应包含溢出信息");
    }

    [TestMethod]
    public void FinalLayout_UsesMeasuredSizes()
    {
        var panel = new SlideMlPanelElement
        {
            Id = "panel1",
            Layout = SlideMlLayoutDirection.Horizontal,
            Gap = 8,
            Children =
            {
                new SlideMlTextElement { Id = "t1", Text = "Hello" },
                new SlideMlTextElement { Id = "t2", Text = "World" },
            },
        };

        var measurements = new SlideMlElementMeasurements(new Dictionary<string, SlideMlMeasureResult>
        {
            ["t1"] = new() { MeasuredWidth = 120, MeasuredHeight = 24 },
            ["t2"] = new() { MeasuredWidth = 100, MeasuredHeight = 24 },
        });

        var page = CreatePage(panel);
        _engine.FinalLayout(page, _context, measurements);

        var t1 = (SlideMlTextElement)panel.Children[0];
        var t2 = (SlideMlTextElement)panel.Children[1];

        Assert.AreEqual(120, t1.ActualWidth, 0.01, "t1 measured width");
        Assert.AreEqual(100, t2.ActualWidth, 0.01, "t2 measured width");
        Assert.AreEqual(120 + 8, t2.LayoutBounds.X, 0.01, "t2 X with measured sizes");
    }

    private static SlideMlPage CreatePage(SlideMlElement child)
    {
        var page = new SlideMlPage();
        page.Children.Add(child);
        return page;
    }
}
