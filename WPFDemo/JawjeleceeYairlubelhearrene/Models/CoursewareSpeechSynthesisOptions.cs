using VolcEngineSdk.OpenSpeech;

namespace JawjeleceeYairlubelhearrene.Models;

internal sealed record CoursewareSpeechSynthesisOptions(
    OpenSpeechAuthentication Authentication,
    string Speaker,
    string AudioFormat = "mp3",
    int SampleRate = 24000,
    string UsageTokensToReturn = "text_words");