using DotNetCampus.Storage.SaveInfos;

namespace DotNetCampus.Storage.Demo.SaveInfos;

[SaveInfoContract("Reference")]
class StorageReferenceSaveInfo : SaveInfo
{
    [SaveInfoMember("Relationships")]
    public List<StorageRelationshipsSaveInfo>? Relationships { get; set; }
}