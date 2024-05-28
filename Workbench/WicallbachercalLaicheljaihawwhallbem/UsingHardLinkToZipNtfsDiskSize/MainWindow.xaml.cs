using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using Microsoft.Extensions.Logging;
using Path = System.IO.Path;
using Microsoft.EntityFrameworkCore;

namespace UsingHardLinkToZipNtfsDiskSize;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void Grid_OnDragEnter(object sender, DragEventArgs e)
    {
        var data = e.Data.GetData(DataFormats.FileDrop) as string[];
        if (data != null && data.Length > 0)
        {
            var folder = data[0];
            if (Directory.Exists(folder))
            {
                _ = StartUsingHardLinkToZipNtfsDiskSize(folder);
            }
        }
    }

    private async ValueTask StartUsingHardLinkToZipNtfsDiskSize(string folder)
    {
        // 转换为日志存储文件夹
        var hexString = Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(folder)));

        var logFolder = Path.GetFullPath(Path.Join(hexString, $"Log_{DateTime.Now:yyyy.MM.dd}"));
        Directory.CreateDirectory(logFolder);

        var logFileStringLoggerWriter = new LogFileStringLoggerWriter(new DirectoryInfo(logFolder));
        var dispatcherStringLoggerWriter = new DispatcherStringLoggerWriter(LogTextBlock);
        using var channelLoggerProvider = new ChannelLoggerProvider(logFileStringLoggerWriter, dispatcherStringLoggerWriter);
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddSimpleConsole(options =>
            {
                options.SingleLine = true;
                options.TimestampFormat = "yyyy.MM.dd HH:mm:ss ";
            });
            // ReSharper disable once AccessToDisposedClosure
            builder.AddProvider(channelLoggerProvider);
        });

        var sqliteFile = Path.Join(hexString, "FileManger.db");
        await using (var fileStorageContext = new FileStorageContext(sqliteFile))
        {
            await fileStorageContext.Database.MigrateAsync();
        }

        var logger = loggerFactory.CreateLogger("");

        logger.LogInformation($"Start zip {folder} folder. LogFolder={logFolder}");

        await Task.Run(async () =>
        {
            await using var fileStorageContext = new FileStorageContext(sqliteFile);

            var provider = new UsingHardLinkToZipNtfsDiskSizeProvider();
            await provider.Start(new DirectoryInfo(folder), fileStorageContext, logger);
        });
    }
}