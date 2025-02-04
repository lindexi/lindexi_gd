// See https://aka.ms/new-console-template for more information

var t = int.MinValue;
var n = t * -1;

var f1 = 1 << 31;

string m = "";

n = -1;
n = t;
n = 0;
for (int i = 0; i < 32; i++)
{
    if ((n & (1 << (31 - i))) == 1)
    {
        m += "1";
    }
    else
    {
        m += "0";
    }
}

Math.Abs(int.MinValue);

Console.WriteLine("Hello, World!");
