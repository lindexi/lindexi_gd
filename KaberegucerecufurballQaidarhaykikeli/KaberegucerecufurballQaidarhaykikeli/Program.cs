using System;
using System.Collections.Generic;
using dotnetCampus.ClrAttachedProperty;

namespace KaberegucerecufurballQaidarhaykikeli
{
    class Program
    {
        static void Main(string[] args)
        {
            var bank = new Bank();
            var person = new Person();
            bank.IdProperty.SetValue(person, "123123");
            var idCard = bank.IdProperty.GetValue(person);
            Console.WriteLine(idCard);
        }
    }

    class Person
    {
        public string Name { get; set; }
    }

    class Bank
    {
        public AttachedProperty<string> IdProperty { get; } = new AttachedProperty<string>();
    }
}
