using Microsoft.Win32;

namespace JawjeleceeYairlubelhearrene.Services;

internal static class FileDialogService
{
    public static string? SelectPowerPointFile()
    {
        var dialog = new OpenFileDialog
        {
            Title = "选择 PowerPoint 文件",
            Filter = "PowerPoint 文件|*.pptx",
            CheckFileExists = true,
            Multiselect = false
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public static string? SelectExecutableFile()
    {
        var dialog = new OpenFileDialog
        {
            Title = "选择可执行文件",
            Filter = "可执行文件|*.exe|所有文件|*.*",
            CheckFileExists = true,
            Multiselect = false
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
