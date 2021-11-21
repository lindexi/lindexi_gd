using dotnetCampus.Cli;

namespace PublishFolderCleaner
{
    public class Options
    {
        [Option('p', "PublishFolder")]
        public string PublishFolder { set; get; } = null!;

        [Option('a', "ApplicationName")]
        public string ApplicationName { set; get; } = null!;
    }
}