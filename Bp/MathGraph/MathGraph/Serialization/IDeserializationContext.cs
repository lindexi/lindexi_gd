using System.Diagnostics.CodeAnalysis;

namespace MathGraph;

public interface IDeserializationContext
{
    bool TryDeserialize(string value, string? type, [NotNullWhen(true)] out object? result);
}