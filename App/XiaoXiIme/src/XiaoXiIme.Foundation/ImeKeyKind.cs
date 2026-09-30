using System.Text.Json.Serialization;

namespace XiaoXiIme.Foundation;

[JsonConverter(typeof(JsonCamelCaseStringEnumConverter))]
public enum ImeKeyKind
{
    Character,
    Backspace,
    Space,
    Enter,
    Escape,
    PreviousCandidate,
    NextCandidate,
    PreviousCandidatePage,
    NextCandidatePage,
    FirstCandidate,
    LastCandidate,
    MoveCompositionCaretLeft,
    MoveCompositionCaretRight,
    CandidateSelection,
    Other
}