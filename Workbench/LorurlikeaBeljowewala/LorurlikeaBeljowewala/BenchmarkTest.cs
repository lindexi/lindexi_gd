using BenchmarkDotNet.Attributes;

namespace LorurlikeaBeljowewala;

public class BenchmarkTest
{
    static BenchmarkTest()
    {
        _yejiwarejaiDearceqofedawu = new YejiwarejaiDearceqofedawu();
        for (int i = 0; i < 100; i++)
        {
            _yejiwarejaiDearceqofedawu.Fxx[i] = new JearhelhairrurHiyawharqall()
            {
                Index = i
            };
        }
    }

    [Benchmark]
    public int Read1()
    {
        var t = 100;
        for (int i = 0; i < 100; i++)
        {
            ref JearhelhairrurHiyawharqall x = ref _yejiwarejaiDearceqofedawu.Fxx[i];
            if (x.Index == t)
            {
                t--;
            }
        }

        return t;
    }

    [Benchmark]
    public int Read2()
    {
        var t = 100;
        for (int i = 0; i < 100; i++)
        {
            JearhelhairrurHiyawharqall x = _yejiwarejaiDearceqofedawu.Fxx[i];
            if (x.Index == t)
            {
                t--;
            }
        }

        return t;
    }

    private static readonly YejiwarejaiDearceqofedawu _yejiwarejaiDearceqofedawu;
}