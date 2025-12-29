using DotNetCampus.Storage.SaveInfos;

namespace DotNetCampus.Storage.Demo.SaveInfos;

[SaveInfoContract("Slide")]
public class SlideSaveInfo : SaveInfo
{
    [SaveInfoMember("Id")]
    public string? Id { get; set; }

    [SaveInfoMember("Elements")]
    public List<ElementSaveInfo>? ElementList { get; set; }
}