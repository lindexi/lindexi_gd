// See https://aka.ms/new-console-template for more information

using System.Text;

var stringBuilder = new StringBuilder();

foreach (var name in Enum.GetNames<Environment.SpecialFolder>())
{
    stringBuilder.AppendLine($"{name} = {Environment.GetFolderPath(Enum.Parse<Environment.SpecialFolder>(name))}");
}

Console.WriteLine(stringBuilder.ToString());

foreach (var name in new[] { "XDG_DATA_HOME", "XDG_CONFIG_HOME", "XDG_CACHE_HOME" })
{
    stringBuilder.AppendLine($"{name} = {Environment.GetEnvironmentVariable(name)}");
}

File.WriteAllText("output.txt", stringBuilder.ToString());

Console.Read();

/*
Desktop = /home/lin/Desktop
Programs = 
MyDocuments = /home/lin/Documents
Personal = /home/lin/Documents
Favorites = 
Startup = 
Recent = 
SendTo = 
StartMenu = 
MyMusic = /home/lin/Music
MyVideos = /home/lin/Videos
DesktopDirectory = /home/lin/Desktop
MyComputer = 
NetworkShortcuts = 
Fonts = 
Templates = /home/lin/.Templates
CommonStartMenu = 
CommonPrograms = 
CommonStartup = 
CommonDesktopDirectory = 
ApplicationData = /home/lin/.config
PrinterShortcuts = 
LocalApplicationData = /home/lin/.local/share
InternetCache = 
Cookies = 
History = 
CommonApplicationData = /usr/share
Windows = 
System = 
ProgramFiles = 
MyPictures = /home/lin/Pictures
UserProfile = /home/lin
SystemX86 = 
ProgramFilesX86 = 
CommonProgramFiles = 
CommonProgramFilesX86 = 
CommonTemplates = 
CommonDocuments = 
CommonAdminTools = 
AdminTools = 
CommonMusic = 
CommonPictures = 
CommonVideos = 
Resources = 
LocalizedResources = 
CommonOemLinks = 
CDBurning = 
XDG_DATA_HOME = /home/lin/.local/share
XDG_CONFIG_HOME = /home/lin/.config
XDG_CACHE_HOME = /home/lin/.cache
 */