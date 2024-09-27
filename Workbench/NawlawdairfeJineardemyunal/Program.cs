// See https://aka.ms/new-console-template for more information

var f1 = new F1();
var f2 = f1.F2;
f2.F3 = new F3();
f1.RaiseFxxEvent();

Console.WriteLine("Hello, World!");

class F1
{
    public F2 F2
    {
        get
        {
            if (_f2 is null)
            {
                var f2 = new F2(this);
                _f2 = f2;
            }

            return _f2;
        }
    }

    private F2? _f2;

    public event EventHandler? FxxEvent;

    public void RaiseFxxEvent()
    {
        FxxEvent?.Invoke(this, EventArgs.Empty);
    }
}

class F2
{
    public F2(F1 f1)
    {
        Id = Interlocked.Increment(ref _count);
        f1.FxxEvent += F1_FxxEvent;
    }

    public int Id { get; }

    public F3 F3 { get; set; } = null!;

    private void F1_FxxEvent(object? sender, EventArgs e)
    {
        F3.Foo();
    }

    private static int _count;
}

class F3
{
    public void Foo()
    {
    }
}