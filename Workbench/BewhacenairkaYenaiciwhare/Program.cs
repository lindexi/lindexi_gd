// See https://aka.ms/new-console-template for more information

StrangeBehavior.Run();

Console.WriteLine("Hello, World!");


    static class StrangeBehavior
    {
        private static volatile bool s_stopWorker = false;

        public static void Run()
        {
            Thread t = new Thread(Worker);
            t.Start();
            Thread.Sleep(5000);
            s_stopWorker = true;//5秒之后，work方法应该结束循环
        }
        private static void Worker()
        {
            int x = 0;
            while (!s_stopWorker)
            {
                x++;
            }
            Console.WriteLine($"worker:stopped when x={x}");//在release模式下，该代码不执行。陷入了死循环出不来
        }
    }