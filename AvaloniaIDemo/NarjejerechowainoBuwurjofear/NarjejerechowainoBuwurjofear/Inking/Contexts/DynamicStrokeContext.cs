namespace NarjejerechowainoBuwurjofear.Inking.Contexts;

class DynamicStrokeContext
{
    public DynamicStrokeContext(InkingInputArgs lastInputArgs)
    {
        LastInputArgs = lastInputArgs;

        Stroke = new SkiaStroke(InkId.NewId());
    }

    public InkingInputArgs LastInputArgs { get; }

    public int Id => LastInputArgs.Id;

    public SkiaStroke Stroke { get; }
}