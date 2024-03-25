namespace NeberwhereneyeWeefemfalaw;

internal class Program
{
    static void Main(string[] args)
    {
        var file = "C:\\lindexi\\Code\\Configuration.coin";
        var text = File.ReadAllText(file);
        text = text.Replace("\r\n", "\n");
        File.WriteAllText(file, text);
    }
}
