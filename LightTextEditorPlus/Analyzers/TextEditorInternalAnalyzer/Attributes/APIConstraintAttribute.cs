using System;

namespace LightTextEditorPlus;

internal class APIConstraintAttribute : Attribute
{
    public APIConstraintAttribute(string name)
    {
        Name = name;
    }

    public string Name { get; }
}