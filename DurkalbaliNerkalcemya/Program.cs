using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DurkalbaliNerkalcemya;

internal class Program
{
    static void Main(string[] args)
    {
        Rect? rect1 = new Rect(10, 10, 10, 10);
        rect1.Value.Union(new Point(100, 100));

        // 10 10 10 10
        Console.WriteLine($"{rect1.Value.X} {rect1.Value.Y} {rect1.Value.Width} {rect1.Value.Height}");

        Rect rect2 = new Rect(10, 10, 10, 10);
        rect2.Union(new Point(100, 100));
        // 10 10 90 90
        Console.WriteLine($"{rect2.X} {rect2.Y} {rect2.Width} {rect2.Height}");

        Foo? foo = new Foo();
        foo.Value.SetNumber(100);
        // 0
        Console.WriteLine(foo.Value.Number);
    }
}

struct Foo
{
    public int Number {  set; get; }

    public void SetNumber(int value) => Number = value;
}