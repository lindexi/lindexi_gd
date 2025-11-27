using DotNetCampus.Storage.SaveInfos;
using DotNetCampus.Storage.Standard;

namespace DotNetCampus.Storage.Demo.SaveInfos;

[SaveInfoContract("Blip")]
public class ImageElementSaveInfo : ElementSaveInfo
{
    [SaveInfoMember("Source")]
    public StorageUri? Source { get; set; }

    [SaveInfoMember("Id")]
    public string? Id { get; set; }
}