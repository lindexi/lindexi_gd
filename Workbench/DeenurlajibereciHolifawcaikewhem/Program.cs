// See https://aka.ms/new-console-template for more information
Console.WriteLine(Sum([1, 2, 3]));


int Sum(Span<int> list)
{
    return list switch
    {
        [] => 0,
        [var first, .. var other] => first + Sum(other),
    };
}