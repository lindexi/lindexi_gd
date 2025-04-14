namespace WejeajaihurleyereRearchicobairnurjeyu;

public interface IFoo
{
}

public interface IF1 : IFoo
{
}

public interface IF2 : IF1
{
}

public class Foo : IF2
{
}

public interface IF3
{

}

public class F1 : Foo, IF3
{
}