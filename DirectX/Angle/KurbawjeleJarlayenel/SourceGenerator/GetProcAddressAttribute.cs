using System;

namespace KurbawjeleJarlayenel.SourceGenerator;

// 现在没有源代码生成器，代码都是从 Avalonia 拷贝

[AttributeUsage(AttributeTargets.Method)]
internal sealed class GetProcAddressAttribute : Attribute
{
    public GetProcAddressAttribute(string proc)
    {

    }

    public GetProcAddressAttribute(string proc, bool optional = false)
    {

    }

    public GetProcAddressAttribute(bool optional)
    {

    }

    public GetProcAddressAttribute()
    {

    }
}