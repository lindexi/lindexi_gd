using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace WejerefonawKaircearwefarce
{
    class Program
    {
        static void Main(string[] args)
        {
            var file = new FileInfo();
            file.LastWriteTime
            var foo = new Foo();
            foo.ReadAll();
        }


    }

    class Foo
    {
        public async void ReadAll()
        {
            while (true)
            {
                var value = await ReadAsync();

                ValueTask<int> ReadAsync()
                {
                    return new ValueTask<int>(0);
                }
            }
        }

        private readonly ConcurrentDictionary<int, Lazy<int>> _keyValues = new ConcurrentDictionary<int, Lazy<int>>();

        private Random Random { get; } = new Random();

    }
}

