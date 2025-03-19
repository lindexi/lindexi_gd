using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DalbawhalelceCujuhurlel.Analyzer;

[Generator(LanguageNames.CSharp)]
public class FooIncrementalGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValueProvider<FooInfo1> foo1ValueProvider = GetProvider();

        IncrementalValueProvider<FooInfo2> foo2ValueProvider = GetProvider2();

        IncrementalValueProvider<(FooInfo1 Left, FooInfo2 Right)> foo1AndFoo2CombineValueProvider = foo1ValueProvider.Combine(foo2ValueProvider);

        IncrementalValuesProvider<FooInfo3> foo3ValuesProvider = foo2ValueProvider.SelectMany
        (
            (FooInfo2 info2, CancellationToken token) =>
            {
                var list = new List<FooInfo3>();
                for (int i = 0; i < info2.Count; i++)
                {
                    list.Add(new FooInfo3());
                }

                return list;
            }
        );
    }

    private IncrementalValueProvider<FooInfo2> GetProvider2()
    {
        throw new NotImplementedException();
    }

    private IncrementalValueProvider<FooInfo1> GetProvider()
    {
        throw new NotImplementedException();
    }
}

readonly record struct FooInfo1(int Number);

readonly record struct FooInfo2(int Count);

readonly record struct FooInfo3();