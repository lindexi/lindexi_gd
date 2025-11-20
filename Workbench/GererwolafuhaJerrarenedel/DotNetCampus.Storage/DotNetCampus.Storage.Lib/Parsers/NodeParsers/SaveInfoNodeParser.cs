using DotNetCampus.Storage.Lib.SaveInfos;

namespace DotNetCampus.Storage.Lib.Parsers.NodeParsers;

public abstract class SaveInfoNodeParser<T> : NodeParser<T>
{
    public abstract SaveInfoContractAttribute ContractAttribute { get; }

    public override string TargetStorageName => ContractAttribute.Name;
}