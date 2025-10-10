namespace CodeSignServerMaster.Contexts;

struct MessageType()
{
    public int HeadLength { get; init; } = 100;
    public int Type { get; init; }
}