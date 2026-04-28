using System.IO;
using System.Threading.Tasks;

namespace SimpleWrite.Business.FileHandlers;

internal interface IFilePickerHandler
{
    Task<FileInfo?> PickSaveFileAsync(DirectoryInfo? suggestedStartLocation = null);

    Task<FileInfo?> PickOpenFileAsync();

    Task<DirectoryInfo?> PickOpenFolderAsync();
}
