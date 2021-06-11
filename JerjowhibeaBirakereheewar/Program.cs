using System;
using System.Runtime.CompilerServices;

namespace JerjowhibeaBirakereheewar
{
    class Program
    {
        static void Main(string[] args)
        {
        }

        public override int GetHashCode()
        {
            //return base.GetHashCode();

            //return RuntimeHelpers.GetHashCode(this);

            var hashCode = new HashCode();
            hashCode.Add(Foo1);
            return hashCode.ToHashCode();
        }

        private double Foo1 { get; }
    }
}
