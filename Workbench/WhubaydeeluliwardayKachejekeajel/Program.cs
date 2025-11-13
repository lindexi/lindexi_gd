F2 f2 = new F2();
F1 f1 = new F1(f2);
f2.RaiseF3();

Console.WriteLine(f1.N1);

struct F1
{
    public F1(F2 f2)
    {
        N1 = 0;
        f2.F3 += F2_F3;
    }
    private void F2_F3(object? sender, EventArgs e)
    {
        N1++;
    }
    public int N1 { get; set; }
}
class F2
{
    public event EventHandler? F3;
    public void RaiseF3()
    {
        F3?.Invoke(this, EventArgs.Empty);
    }
}