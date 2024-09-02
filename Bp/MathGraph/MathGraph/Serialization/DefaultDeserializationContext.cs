using System.Diagnostics.CodeAnalysis;

namespace MathGraph;

class DefaultDeserializationContext : IDeserializationContext
{
    public bool TryDeserialize(string value, string? type, [NotNullWhen(true)] out object? result)
    {
        result = null;
        return false;
    }
}