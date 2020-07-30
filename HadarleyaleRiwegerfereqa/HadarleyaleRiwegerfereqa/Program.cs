using System;
using dotnetCampus.ClrAttachedProperty;

namespace HadarleyaleRiwegerfereqa
{
    class Program
    {
        static void Main(string[] args)
        {
            var property = new AttachedProperty<bool>();

            object foo = new object();
            property.SetValue(foo, true);

            Console.WriteLine(property.GetValue(foo));
        }
    }
}
