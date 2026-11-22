namespace XiaoXiIme.ImeModule;

internal sealed class ImeIpcCircuit(TimeProvider clock)
{
    internal static ImeIpcCircuit Shared { get; } = new(TimeProvider.System);

    private readonly Lock _sync = new();
    private int _timeouts;
    private long _opened;
    private long _generation;
    private bool _open;

    internal bool TryEnter(out long generation)
    {
        lock (_sync)
        {
            if (_open && clock.GetElapsedTime(_opened) >= TimeSpan.FromMinutes(1))
            {
                _open = false;
                _timeouts = 0;
            }
            generation = _generation;
            return !_open;
        }
    }

    internal bool Complete(long generation, bool timedOut)
    {
        lock (_sync)
        {
            if (_open || generation != _generation)
            {
                return false;
            }
            _timeouts = timedOut ? _timeouts + 1 : 0;
            if (_timeouts == 5)
            {
                _opened = clock.GetTimestamp();
                _generation++;
                _open = true;
                return true;
            }
            return false;
        }
    }
}
