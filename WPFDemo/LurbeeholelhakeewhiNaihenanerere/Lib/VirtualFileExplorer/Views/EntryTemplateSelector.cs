using System.Windows;
using System.Windows.Controls;
using VirtualFileExplorer.Core;

namespace VirtualFileExplorer.Views;

public class EntryTemplateSelector : DataTemplateSelector
{
    public DataTemplate? FileTemplate { get; set; }

    public DataTemplate? FolderTemplate { get; set; }

    public DataTemplate? DefaultTemplate { get; set; }

    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
    {
        return item switch
        {
            VirtualFolderInfo => FolderTemplate ?? DefaultTemplate,
            VirtualFileInfo => FileTemplate ?? DefaultTemplate,
            _ => DefaultTemplate,
        };
    }
}
