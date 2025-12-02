// See https://aka.ms/new-console-template for more information
Fxx();
Console.WriteLine("Hello, World!");
Console.Read();

void Fxx()
{
    bool isFxxx = false;

    async Task F1()
    {
        await Task.Delay(300);
        Console.WriteLine(isFxxx);
    }

    _ = F1();
    isFxxx = true;
}