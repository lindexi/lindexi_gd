// See https://aka.ms/new-console-template for more information

// 在 m=50 人范围中，抽选 n=2 次时，抽选相同 k=1 的情况
var m = 50d;
var k = 1d;
var n = 2d;

for (int i = 2; i < m; i++)
{
    n = i;
    var result = 1 - Factorial(m) / (Factorial(k) * Factorial(m - k)) * Factorial(n) / (Factorial(k) * Factorial(n - k)) / Factorial(m) * Factorial(m - n);
    Console.WriteLine($"在 m={m} 人范围中，抽选 n={n} 次时，抽选相同 k=1 的情况，概率是 {result}");
}

Console.WriteLine("Hello, World!");


double Factorial(double number)
{
    if (number == 0)
    {
        return 1;
    }
    else
    {
        return number * Factorial(number - 1);
    }
}