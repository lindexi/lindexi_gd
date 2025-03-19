using Lindexi;

namespace NayijainawNerkanekajawi;

public interface IFoo
{
}

public class F1 : IFoo
{
    public F1(IContext context)
    {
        // 忽略其他代码
    }
}

public class F2 : IFoo
{
    public F2(IContext context)
    {
        // 忽略其他代码
    }
}

public class F3 : IFoo
{
    public F3()
    {
        // 忽略其他代码
    }
}

public interface IContext
{
    // 忽略其他代码
}

public static partial class FooCollection
{
    [Collection]
    public static partial IEnumerable<Func<IContext, IFoo>> GetFooCreatorList();
}