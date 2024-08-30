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
    public MathGraphElement(T value)
    {
        Value = value;

        if (Value is IMathGraphElementSensitive<T> sensitive)
        {
            sensitive.MathGraphElement = this;
        }
    }

    public T Value { get; set; }

    public List<MathGraphElement<T>> OutElementList { get; } = [];

    public List<MathGraphElement<T>> InElementList { get; } = [];
}