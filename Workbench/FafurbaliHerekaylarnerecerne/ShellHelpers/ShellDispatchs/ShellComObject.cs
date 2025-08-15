namespace FafurbaliHerekaylarnerecerne;

public unsafe class ShellComObject : TheComObject
{
    public ShellComObject() : base(new Guid("13709620-C279-11CE-A49E-444553540000"), typeof(IShellDispatch4).GUID)
    {
    }

    public IShellDispatch4 AsIShellDispatch4() => As<IShellDispatch4>()!;
}