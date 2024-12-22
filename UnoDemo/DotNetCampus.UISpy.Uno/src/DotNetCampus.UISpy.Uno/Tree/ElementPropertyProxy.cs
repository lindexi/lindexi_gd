using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetCampus.UISpy.Uno.Tree;

public readonly record struct ElementPropertyProxy(
    DependencyObject Element,
    string PropertyName,
    object? Value,
    string PropertyTypeName)
{
    public bool IsFailed { get; init; }

    public bool IsNotImplemented { get; init; }
}
