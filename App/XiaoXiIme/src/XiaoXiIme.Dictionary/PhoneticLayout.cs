using System.Collections.Frozen;

namespace XiaoXiIme.Dictionary;

/// <summary>Immutable component mappings; observed keys do not imply a complete product input policy.</summary>
public sealed class PhoneticLayout
{
    /// <summary>Copies mappings, preserving explicitly supplied symbol keys and undefined zero-initial policy.</summary>
    public PhoneticLayout
    (
        IReadOnlyDictionary<string, char> initials, IReadOnlyDictionary<string, char> finals,
        IReadOnlyDictionary<string, string>? zeroInitialCodes = null,
        IReadOnlyDictionary<string, char>? observedFinals = null
    )
    {
        ArgumentNullException.ThrowIfNull(initials);
        ArgumentNullException.ThrowIfNull(finals);
        Initials = initials.ToFrozenDictionary(StringComparer.Ordinal);
        Finals = finals.ToFrozenDictionary(StringComparer.Ordinal);
        ObservedFinals = (observedFinals ?? finals).ToFrozenDictionary(StringComparer.Ordinal);
        ZeroInitialCodes = zeroInitialCodes?.ToFrozenDictionary(StringComparer.Ordinal);
    }

    /// <summary>Gets confirmed initial keys.</summary>
    public IReadOnlyDictionary<string, char> Initials { get; }

    /// <summary>Gets final keys usable under the current projection policy.</summary>
    public IReadOnlyDictionary<string, char> Finals { get; }

    /// <summary>Gets the complete image transcription, including extensions not yet enabled by normalization policy.</summary>
    public IReadOnlyDictionary<string, char> ObservedFinals { get; }

    /// <summary>Gets explicit zero-initial codes, or null when no policy is defined.</summary>
    public IReadOnlyDictionary<string, string>? ZeroInitialCodes { get; }

    /// <summary>Projects already normalized components without alias fallback or inferred zero-initial rules.</summary>
    public bool TryProjectComponents(string initial, string final, out string code)
    {
        ArgumentNullException.ThrowIfNull(initial);
        ArgumentNullException.ThrowIfNull(final);
        code = string.Empty;
        if (initial.Length == 0)
        {
            if (ZeroInitialCodes is null || !ZeroInitialCodes.TryGetValue(final, out var zeroCode))
            {
                return false;
            }

            code = zeroCode;
            return true;
        }

        if (!Initials.TryGetValue(initial, out var initialKey) || !Finals.TryGetValue(final, out var finalKey))
        {
            return false;
        }

        code = string.Concat(initialKey, finalKey);
        return true;
    }
}