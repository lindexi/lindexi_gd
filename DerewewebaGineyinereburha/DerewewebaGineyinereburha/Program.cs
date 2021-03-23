namespace DerewewebaGineyinereburha
{
    class Program
    {
        static void Main(string[] args)
        {
            var hahicafojuwembaKuleajigaideeba = new HahicafojuwembaKuleajigaideeba(1000, 1000);
            hahicafojuwembaKuleajigaideeba.Build();

            var hilerehanuwereleQeyifinu = new HilerehanuwereleQeyifinu();
            hilerehanuwereleQeyifinu.Draw(hahicafojuwembaKuleajigaideeba);

            for (int i = 0; i < 1000; i++)
            {
                hahicafojuwembaKuleajigaideeba.Proc();
            }
        }
    }
}
