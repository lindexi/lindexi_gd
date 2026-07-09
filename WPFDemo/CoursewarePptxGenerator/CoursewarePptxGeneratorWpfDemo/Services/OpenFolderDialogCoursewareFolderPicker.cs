using Microsoft.Win32;

namespace CoursewarePptxGeneratorWpfDemo.Services;

/// <summary>
/// Selects a courseware folder by using the Windows open folder dialog.
/// </summary>
public sealed class OpenFolderDialogCoursewareFolderPicker : ICoursewareFolderPicker
{
    /// <inheritdoc />
    public string? PickCoursewareFolder()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "选择课件 Markdown 导出文件夹",
            Multiselect = false,
        };

        return dialog.ShowDialog() == true ? dialog.FolderName : null;
    }
}
