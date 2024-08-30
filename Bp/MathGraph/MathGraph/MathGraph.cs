using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MathGraph;

public class MathGraph
{
}

public class MathGraphElement<T>
{
    public T Value { get; set; }
    public MathGraphElement<T>? Next { get; set; }
    public MathGraphElement<T>? Previous { get; set; }

    public MathGraphElement(T value)
    {
        Value = value;
    }
}