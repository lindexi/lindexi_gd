using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace SimpleWrite.Business.FileHandlers;

class SaveFilePickerHandler : ISaveFilePickerHandler
{
    public SaveFilePickerHandler(TopLevel topLevel)
    {
        _topLevel = topLevel;
    }

    private readonly TopLevel _topLevel;

    public async Task<FileInfo?> PickSaveFileAsync()
    {
        var filePickerSaveOptions = new FilePickerSaveOptions()
        {
        };
        var pickResult = await _topLevel.StorageProvider.SaveFilePickerAsync(filePickerSaveOptions);
        var localFilePath = pickResult?.TryGetLocalPath();
        if (localFilePath is null)
        {
            return null;
        }

        return new FileInfo(localFilePath);
    }
}
