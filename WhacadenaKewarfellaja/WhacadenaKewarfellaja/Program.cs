namespace WhacadenaKewarfellaja
{
    public static partial class Program
    {
        public static void Main(string[] args)
        {
            HelloFrom("Fxx");

            Console.WriteLine("Hello, World!");
        }

        static partial void HelloFrom(string name);
    }
}