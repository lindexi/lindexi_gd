using System.IO;
using System.Threading.Tasks;

namespace SimpleWrite.Business.FileHandlers;

internal interface IOpenFilePickerHandler
{
    Task<FileInfo?> PickOpenFileAsync();
}
