using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace SimpleWrite.Business.FileHandlers;

class OpenFilePickerHandler : IOpenFilePickerHandler
{
    public OpenFilePickerHandler(TopLevel topLevel)
    {
        _topLevel = topLevel;
    }

    private readonly TopLevel _topLevel;

    public async Task<FileInfo?> PickOpenFileAsync()
    {
        var pickResultList = await _topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false
        });

        var pickResult = pickResultList.Count > 0 ? pickResultList[0] : null;
        var localFilePath = pickResult?.TryGetLocalPath();
        if (localFilePath is null)
        {
            return null;
        }

        return new FileInfo(localFilePath);
    }
}
