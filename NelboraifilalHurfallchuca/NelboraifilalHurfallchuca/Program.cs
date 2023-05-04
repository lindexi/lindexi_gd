// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");

var start = new DateTime(2023, 3, 2);

for(int i = 0;i < 60; i++)
{
    var end = new DateTime(2023, 4, 5);
    end = end.AddDays(i);

    var day = (int) (end - start).TotalDays;

    var n = day % 8;

    Console.WriteLine(end);

    Console.WriteLine(end.DayOfWeek);

    if (n == 0 || n == 1)
    {
        Console.WriteLine("1");
    }
    else if (n == 2 || n == 3)
    {
        Console.WriteLine("2");
    }
    else if (n == 4 || n == 5)
    {
        Console.WriteLine("3");
    }
    else if (n == 6 || n == 7)
    {
        if(end.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            Console.WriteLine("-");
        }
        else
        {
            Console.WriteLine("0");
        }
    }
}