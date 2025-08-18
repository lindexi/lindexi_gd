using Lindexi;

namespace JicanerekeaFawbairbalbayhearwear;

public interface IFoo
{
}

public class F1 : IFoo
{
    public F1(IContext context)
    {
    }
}

public class F2 : IFoo
{
    public F2(IContext context)
    {
    }
}

public class F3 : IFoo
{
    public F3()
    {
    }
}

public interface IContext
{
}

public static partial class FooCollection
{
    [Collection]
    public static partial IEnumerable<Func<IContext, IFoo>> GetFooCreatorList();
}

//public static partial class FooCollection
//{
//    public static partial IEnumerable<Func<IContext, IFoo>> GetFooCreatorList()
//    {
//        yield return context => new F1(context);
//        yield return context => new F2(context);
//        yield return context => new F3(context);
//    }
//}