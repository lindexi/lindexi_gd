namespace JawjeleceeYairlubelhearrene.Models;

internal sealed record SpeechVideoGenerationResult(
    System.IO.FileInfo OutputVideoFile,
    System.IO.DirectoryInfo OutputDirectory,
    CoursewareSpeechInfo CoursewareSpeechInfo);