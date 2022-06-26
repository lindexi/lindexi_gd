// See https://aka.ms/new-console-template for more information
using BenchmarkDotNet.Attributes;

using System.Reflection;

namespace LahawdairyurjiWuraycinu;

public class CommandValueSerializer
{
    [Benchmark]
    [ArgumentsSource(nameof(GetArguments))]
    public bool FindExistMemberByGetMember(string localName)
    {
        var ownerType = typeof(TestType);
        var member = ownerType.GetMember(localName, BindingFlags.Public | BindingFlags.Static);
    }

    [Benchmark(Baseline = true)]
    public bool FindExistMemberByGetPropertyAndField(string localName)
    {
        var ownerType = typeof(TestType);
        // Get them from Properties
        PropertyInfo? propertyInfo = ownerType.GetProperty(localName, BindingFlags.Public | BindingFlags.Static);
        if (propertyInfo != null)
            return true;

        // Get them from Fields (ScrollViewer.PageDownCommand is a static readonly field
        FieldInfo? fieldInfo = ownerType.GetField(localName, BindingFlags.Static | BindingFlags.Public);
        if (fieldInfo != null)
            return true;
        return false;
    }

    public IEnumerable<string> GetArguments()
    {
        yield return "TheProperty";
        yield return "NotExistProperty";
        yield return "TheField";
        yield return "NotExistField";
    }
}
