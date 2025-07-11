using System.Reflection;

namespace FukokayrawobelbayNadojearchehi;

public class FooAttribute : Attribute, ITestDataSource
{
    public IEnumerable<object?[]> GetData(MethodInfo methodInfo)
    {
        return [[123]];
    }

    public string? GetDisplayName(MethodInfo methodInfo, object?[]? data)
    {
        return "Fxxx";
    }
}