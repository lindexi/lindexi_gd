// See https://aka.ms/new-console-template for more information
using System;

public class Program
{
    public static void Main(string[] args)
    {
        IFoo foo = null;

        var f1 = foo;

        if (foo is var f2)
        {
            Console.WriteLine($"居然进来了。 F2={f2}");
        }

        if (foo is IFoo f3)
        {
            Console.WriteLine($"不进来");
        }

        Console.WriteLine("Hello, World!");
    }

    static bool IsFoo()
    {
        if (GetFxx() is var fxx)
        {
            if (CheckXx(fxx) is var result)
            {
                return DoXxx(result);
            }
        }

        return false;
    }

    private static bool DoXxx(object result)
    {
        throw new NotImplementedException();
    }

    private static object CheckXx(object fxx)
    {
        throw new NotImplementedException();
    }

    private static object GetFxx()
    {
        throw new NotImplementedException();
    }
}

interface IFoo
{
}