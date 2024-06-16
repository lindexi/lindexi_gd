// See https://aka.ms/new-console-template for more information

Console.Write("Hello, World!");

while (true)
{
    await Task.Delay(1000);

    Console.SetCursorPosition(0, Console.CursorTop);//将光标至于当前行的开始位置
    Console.Write(new string(' ', Console.WindowWidth));//用空格将当前行填满，相当于清除当前行
    Console.SetCursorPosition(0, Console.CursorTop);//将光标至于当前行的开始位置

    for (int i = 0; i < Console.WindowWidth; i++)
    {
        await Task.Delay(10);
        Console.Write("|");
    }
}

