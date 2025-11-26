using DotNetCampus.Storage.SaveInfos;

namespace DotNetCampus.Storage.Demo.SaveInfos;

[SaveInfoContract("Blip")]
public class ImageElementSaveInfo : ElementSaveInfo
{
    [SaveInfoMember("Source")]
    public string? Source { get; set; }
}