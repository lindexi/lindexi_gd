using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using LibGit2Sharp;

namespace ChiwolufaiHaihurcheelair;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var currentFolder = System.IO.Path.GetFullPath(".");
        while (currentFolder != null)
        {
            var gitFolderOrFile = System.IO.Path.Combine(currentFolder, ".git");
            if (System.IO.Directory.Exists(gitFolderOrFile) || File.Exists(gitFolderOrFile))
            {
                break;
            }
            currentFolder = System.IO.Directory.GetParent(currentFolder)?.FullName;
        }

        if (currentFolder == null)
        {
            return;
        }

        var gitRepositoryFolderOrFile = System.IO.Path.Combine(currentFolder, ".git");
        var gitRepositoryFolder = gitRepositoryFolderOrFile;

        if (File.Exists(gitRepositoryFolderOrFile))
        {
            return;
        }

        var repository = new Repository(gitRepositoryFolder);
        var commit = repository.Commits.FirstOrDefault();
        CommitTextBlock.Text = commit?.Id?.Sha ?? string.Empty;
    }
}