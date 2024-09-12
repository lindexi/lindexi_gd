using System.Diagnostics.CodeAnalysis;

namespace MathGraph.Serialization;

public interface IDeserializationContext
{
    bool TryDeserialize(string value, string? type, [NotNullWhen(true)] out object? result);
}