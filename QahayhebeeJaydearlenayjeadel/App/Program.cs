// See https://aka.ms/new-console-template for more information

foreach (var name in InternalsVisibleToHelper.GetAllInternalsVisibleFromAssemblyName())
{
    Console.WriteLine(name);
}
