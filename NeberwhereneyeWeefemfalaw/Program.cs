using dotnetCampus.Configurations.Core;

namespace NeberwhereneyeWeefemfalaw;

internal class Program
{
    static void Main(string[] args)
    {
        var file = "C:\\lindexi\\Code\\Configuration.coin";

        var fileConfigurationRepo = ConfigurationFactory.FromFile(file);
        var appConfigurator = fileConfigurationRepo.CreateAppConfigurator();
        string configurationString = appConfigurator.Default["NasSetupRepositoryFolderPath"];
        Console.WriteLine(Directory.Exists(configurationString));

        //var text = File.ReadAllText(file);
        //var index = text.IndexOf("Y:\\Setup");
        //if (index > 0)
        //{
        //    text = text.Substring(index);
        //}
        //text = text.Replace("\r\n", "\n");
        //File.WriteAllText(file, text);
    }
}
