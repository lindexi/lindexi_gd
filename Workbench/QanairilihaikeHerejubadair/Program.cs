// See https://aka.ms/new-console-template for more information

// 在 m=50 人范围中，抽选 n=2 次时，抽选相同 k=1 的情况
var m = 50d;
var k = 1d;
var n = 2d;

for (int i = 2; i < m; i++)
{
    k = i;
    var result = 1 - (Factorial(m - n * k - 1)) / (Math.Pow(m, n - 1) * Factorial(m - n * (k + 1)));
    Console.WriteLine($"在 m={m} 人范围中，抽选 k={k} 次时，抽选相同 n={n} 的情况，概率是 {result}");
}


Console.WriteLine("Hello, World!");


double Factorial(double number)
{
    if (number < 0)
    {

    }
    if (number == 0)
    {
        return 1;
    }
    else
    {
        return number * Factorial(number - 1);
    }
}