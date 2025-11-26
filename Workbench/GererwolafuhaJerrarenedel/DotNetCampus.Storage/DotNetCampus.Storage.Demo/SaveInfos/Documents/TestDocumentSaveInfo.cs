using DotNetCampus.Storage.SaveInfos;

namespace DotNetCampus.Storage.Demo.SaveInfos;

[SaveInfoContract("Document")]
class TestDocumentSaveInfo : SaveInfo
{
    [SaveInfoMember("Name")]
    public string? Name { get; set; }

    [SaveInfoMember("Creator")]
    public string? Creator { get; set; }

    [SaveInfoMember("DocumentVersion")]
    public string? DocumentVersion { get; set; }
}