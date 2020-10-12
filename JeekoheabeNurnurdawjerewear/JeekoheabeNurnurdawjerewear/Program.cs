using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JeekoheabeNurnurdawjerewear
{
    class Program
    {
        static void Main(string[] args)
        {
            var maxCount = 100000000;

            var threadCount = 100;

            var list = new ConcurrentWriteOnlyBag<int>(maxCount);
            var taskList = new Task[threadCount];

            for (int j = 0; j < threadCount; j++)
            {
                var n = j;
                taskList[n] = Task.Run(() =>
                {
                    for (int i = 0; i < maxCount / threadCount; i++)
                    {
                        var value = maxCount / threadCount * n + i;
                        list.Add(value);
                    }
                });
            }

            Task.WaitAll(taskList);

            var hashSet = new HashSet<int>();
            var readOnlyCollection = (int[]) list.GetReadOnlyCollection();
            for (int i = 0; i < readOnlyCollection.Length; i++)
            {
                var value = readOnlyCollection[i];

                if (hashSet.Add(value))
                {
                }
                else
                {
                    // 重复的值
                }
            }
        }
    }
}