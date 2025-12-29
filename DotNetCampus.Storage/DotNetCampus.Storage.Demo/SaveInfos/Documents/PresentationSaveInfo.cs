using DotNetCampus.Storage.SaveInfos;

namespace DotNetCampus.Storage.Demo.SaveInfos;

[SaveInfoContract("Board")]
public class PresentationSaveInfo : SaveInfo
{
    [SaveInfoMember("Slides")]
    public List<string>? SlideIdList { get; set; }
}