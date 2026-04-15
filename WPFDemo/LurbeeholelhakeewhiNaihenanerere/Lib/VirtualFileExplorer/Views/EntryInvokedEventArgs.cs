using VirtualFileExplorer.Core;

namespace VirtualFileExplorer.Views;

public sealed class EntryInvokedEventArgs : EventArgs
{
    public EntryInvokedEventArgs(VirtualFileSystemEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        Entry = entry;
    }

    public VirtualFileSystemEntry Entry { get; }
}
