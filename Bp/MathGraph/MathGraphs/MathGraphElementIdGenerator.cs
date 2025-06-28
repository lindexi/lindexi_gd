namespace MathGraph;

static class MathGraphElementIdGenerator
{
    private static ulong _idCounter = 0;

    public static string GenerateId()
    {
        return Interlocked.Increment(ref _idCounter).ToString();
    }
}