namespace FawfijeefawkerehearwhuRalldawnola;

class Program
{
    static void Main(string[] args)
    {
        _ = DoXxx();
        Console.WriteLine("Hello");
    }

    private static async Task DoXxx()
    {
        Throw();

        await Task.Delay(100);
    }

    private static void Throw()
    {
        throw new Exception();
    }
}