namespace XiaoXiIme.TsfModule;

internal static class TsfUnmanagedBoundary
{
    public static int Invoke(Func<int> callback)
    {
        try
        {
            return callback();
        }
        catch (OutOfMemoryException)
        {
            return TsfHResults.E_OUTOFMEMORY;
        }
        catch
        {
            return TsfHResults.E_FAIL;
        }
    }
}