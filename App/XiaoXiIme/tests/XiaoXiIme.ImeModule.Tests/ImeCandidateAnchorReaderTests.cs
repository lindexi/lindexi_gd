using XiaoXiIme.ImeInterop;

namespace XiaoXiIme.ImeModule.Tests;

public class ImeCandidateAnchorReaderTests
{
    [Theory]
    [InlineData(0u, 2u)]
    [InlineData(8u, 2u)]
    [InlineData(16u, 0u)]
    [InlineData(16u, 64u)]
    public void Composition_UninitializedOrNonPointFormIsUnavailable(uint initialization, uint style)
    {
        var context = new InputContext
        {
            FdwInit = initialization,
            CompositionForm = new() { Style = style, CurrentPosition = new() { X = 123, Y = 456 } },
        };

        Assert.Null(ImeCandidateAnchorReader.ReadCompositionForm(context));
    }

    [Theory]
    [InlineData(16u, 2u, 0, 0)]
    [InlineData(24u, 32u, -100, 25)]
    public void Composition_InitializedPointPreservesClientCoordinates(uint initialization, uint style, int x, int y)
    {
        var context = new InputContext
        {
            FdwInit = initialization,
            CompositionForm = new() { Style = style, CurrentPosition = new() { X = x, Y = y } },
        };

        Assert.Equal((x, y, y, "composition-position"), ImeCandidateAnchorReader.ReadCompositionForm(context));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(-50, 100)]
    public unsafe void Candidate_PositionDoesNotRequireCompositionInitialization(int x, int y)
    {
        var context = new InputContext();
        var form = (CandidateForm*)context.CandidateForms;
        *form = new() { Style = 0x40, CurrentPosition = new() { X = x, Y = y } };

        Assert.Equal((x, y, y, "candidate-position"), ImeCandidateAnchorReader.ReadCandidateForm(context));
    }

    [Theory]
    [InlineData(1u, 64u)]
    [InlineData(0u, 0u)]
    [InlineData(0u, 2u)]
    public unsafe void Candidate_NonzeroListOrUnsupportedStyleIsUnavailable(uint index, uint style)
    {
        var context = new InputContext();
        var form = (CandidateForm*)context.CandidateForms;
        *form = new() { Index = index, Style = style };

        Assert.Null(ImeCandidateAnchorReader.ReadCandidateForm(context));
    }

    [Fact]
    public unsafe void Candidate_ExcludeUsesCurrentXAndRectangleVerticalBounds()
    {
        var context = new InputContext();
        var form = (CandidateForm*)context.CandidateForms;
        *form = new()
        {
            Style = 0x80,
            CurrentPosition = new() { X = 42, Y = 999 },
            Area = new() { Left = 10, Top = 20, Right = 70, Bottom = 80 },
        };

        Assert.Equal((42, 80, 20, "candidate-exclude"), ImeCandidateAnchorReader.ReadCandidateForm(context));
    }

    [Theory]
    [InlineData(70, 20, 10, 80)]
    [InlineData(10, 80, 70, 20)]
    public unsafe void Candidate_InvertedExclusionRectangleIsUnavailable(int left, int top, int right, int bottom)
    {
        var context = new InputContext();
        var form = (CandidateForm*)context.CandidateForms;
        *form = new()
        {
            Style = 0x80,
            Area = new() { Left = left, Top = top, Right = right, Bottom = bottom },
        };

        Assert.Null(ImeCandidateAnchorReader.ReadCandidateForm(context));
    }
}
