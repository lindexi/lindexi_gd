using System;

namespace BairbacearbakurgaicairCearlellerfenall
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
        }
    }

    abstract class Animal
    {
        public virtual Food GetFood()
        {
            return null;
        }

    }

    class Tiger : Animal
    {
        public new Food GetFood() => new Meat();
    }

    public class Food
    {

    }

    public class Meat : Food
    {

    }
}
