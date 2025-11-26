using DotNetCampus.Storage.SaveInfos;

namespace DotNetCampus.Storage.Demo.SaveInfos;

[SaveInfoContract("Relationship")]
class StorageRelationshipsSaveInfo : SaveInfo
{
    /*      <Id>954ebb106c53a3caacf6f65eec5a8171</Id>
       <Target>Media\954ebb106c53a3caacf6f65eec5a8171.png</Target>
       <Hash>8c40fff0a32df8304bec726282d933a1</Hash>*/

    [SaveInfoMember("Id")]
    public string? Id { get; set; }

    [SaveInfoMember("Target")]
    public string? Target { get; set; }

    [SaveInfoMember("Hash")]
    public string? Hash { get; set; }
}