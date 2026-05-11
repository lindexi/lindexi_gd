namespace JawjeleceeYairlubelhearrene.Models;

internal sealed record CoursewareSpeechSlideInfo(string PlainScriptText, System.IO.FileInfo SlideThumbnailFile);

internal sealed record CoursewareSpeechGenerationProgress(int SlideNumber, string PlainScriptText);