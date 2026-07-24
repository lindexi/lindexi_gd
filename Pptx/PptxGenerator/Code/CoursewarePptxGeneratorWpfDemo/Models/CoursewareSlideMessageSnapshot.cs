using CoursewarePptxGeneratorWpfDemo.ViewModels;

namespace CoursewarePptxGeneratorWpfDemo.Models;

/// <summary>
/// Captures the immutable visible page draft and attachments used by one send operation.
/// </summary>
public sealed record CoursewareSlideMessageSnapshot(
    string Message,
    long DraftRevision,
    bool IsFirstMessage,
    bool AttachPreview,
    IReadOnlyList<CoursewareChatImageAttachmentViewModel> Attachments);
