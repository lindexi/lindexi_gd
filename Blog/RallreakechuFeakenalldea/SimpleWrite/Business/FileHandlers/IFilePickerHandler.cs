using System.IO;
using System.Threading.Tasks;

namespace SimpleWrite.Business.FileHandlers;

internal interface IFilePickerHandler
{
    Task<FileInfo?> PickSaveFileAsync();

    Task<FileInfo?> PickOpenFileAsync();
}
