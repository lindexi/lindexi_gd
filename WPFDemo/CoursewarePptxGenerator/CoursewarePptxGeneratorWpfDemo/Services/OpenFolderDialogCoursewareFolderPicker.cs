using System.IO;
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
        var dialog = new OpenFileDialog
        {
            Title = "选择课件 Markdown 导出文件夹",
            CheckFileExists = false,
            ValidateNames = false,
            FileName = "选择文件夹",
        };

        return dialog.ShowDialog() == true ? Path.GetDirectoryName(dialog.FileName) : null;
    }
}
