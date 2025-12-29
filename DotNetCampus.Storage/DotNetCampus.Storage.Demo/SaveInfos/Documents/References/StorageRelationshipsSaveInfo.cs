using DotNetCampus.Storage.SaveInfos;

namespace DotNetCampus.Storage.Demo.SaveInfos;

[SaveInfoContract("Relationship")]
class StorageRelationshipsSaveInfo : SaveInfo
{
    [SaveInfoMember("Id")]
    public string? Id { get; set; }

    [SaveInfoMember("Target")]
    public string? Target { get; set; }

    [SaveInfoMember("Hash")]
    public string? Hash { get; set; }
}