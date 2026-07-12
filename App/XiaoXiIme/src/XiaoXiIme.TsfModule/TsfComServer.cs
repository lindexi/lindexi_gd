namespace XiaoXiIme.TsfModule;

internal static unsafe class TsfComServer
{
    private static int s_lockCount;
    private static int s_objectCount;

    public static bool CanUnload => false;

    internal static int LockCount => Volatile.Read(ref s_lockCount);

    internal static int ObjectCount => Volatile.Read(ref s_objectCount);

    public static int TryCreateClassFactory(Guid interfaceId, nint* result)
    {
        if (result is null)
        {
            return TsfHResults.E_POINTER;
        }

        return TsfClassFactory.TryCreate(interfaceId, result);
    }

    public static void AddObject() => Interlocked.Increment(ref s_objectCount);

    public static void ReleaseObject() => DecrementNonNegative(ref s_objectCount);

    public static void LockServer(bool value)
    {
        if (value)
        {
            Interlocked.Increment(ref s_lockCount);
        }
        else
        {
            DecrementNonNegative(ref s_lockCount);
        }
    }

    private static void DecrementNonNegative(ref int count)
    {
        while (true)
        {
            var current = Volatile.Read(ref count);
            if (current == 0)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref count, current - 1, current) == current)
            {
                return;
            }
        }
    }
}
