using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibDemo;

public static class Runner
{
    public static void SetAppRunner(IAppRunner appRunner)
    {
        _appRunner = appRunner;
    }

    public static void Run() => _appRunner?.Run();

    private static IAppRunner? _appRunner;
}