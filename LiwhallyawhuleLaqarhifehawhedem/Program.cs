// See https://aka.ms/new-console-template for more information

using LiwhallyawhuleLaqarhifehawhedem.PulseAudio;

if (!OperatingSystem.IsLinux())
{
    return;
}

var pulseAudioVolumeManager = new PulseAudioVolumeManager();
await pulseAudioVolumeManager.Init();

pulseAudioVolumeManager.VolumeChanged += (sender, volume) =>
{
    Console.WriteLine($"音量变化，当前音量：{volume}");
};

pulseAudioVolumeManager.MuteChanged += (sender, isMute) =>
{
    Console.WriteLine($"静音变化，当前是否静音：{isMute}");
};

while (true)
{
    Console.WriteLine($"是否静音：{await pulseAudioVolumeManager.GetMute()}; 音量：{await pulseAudioVolumeManager.GetVolume()}");

    Console.WriteLine($"输入数字修改音量，输入 y/n 设置是否静音");
    var line = Console.ReadLine();
    if (int.TryParse(line, out var n))
    {
        Console.WriteLine($"设置音量为：{n}");
        await pulseAudioVolumeManager.SetVolume(n);
    }
    else if(line is not null)
    {
        var text = line.ToLowerInvariant();
        if (text == "y")
        {
            Console.WriteLine($"设置是否静音：是");
            await pulseAudioVolumeManager.SetMute(true);
        }
        else if (text == "n")
        {
            Console.WriteLine($"设置是否静音：否");
            await pulseAudioVolumeManager.SetMute(false);
        }
    }
}
