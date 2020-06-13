using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace CallbadojuBaheanurjair
{
    class Program
    {
        static void Main(string[] args)
        {
            //var listRemoveTest = new ListRemoveTest();
            //var list = listRemoveTest.GetArgumentList().First();
            //listRemoveTest.RemoveFromEnd(list);

            //if (list.All(temp => temp.N <= ListRemoveTest.MaxCount / 2))
            //{

            //}

            BenchmarkRunner.Run<ListRemoveTest>();
        }
    }

    public class ListRemoveTest
    {
        [Benchmark]
        [ArgumentsSource(nameof(GetArgumentList))]
        public void RemoveAll(List<Foo> list)
        {
            list.RemoveAll(temp => temp.N > MaxCount / 2);
        }

        [Benchmark]
        [ArgumentsSource(nameof(GetArgumentList))]
        public void RemoveFromStart(List<Foo> list)
        {
            for (var i = 0; i < list.Count; i++)
            {
                if (list[i].N > MaxCount / 2)
                {
                    list.RemoveAt(i);
                    i--;
                }
            }
        }

        [Benchmark]
        [ArgumentsSource(nameof(GetArgumentList))]
        public void RemoveFromEnd(List<Foo> list)
        {
            for (var i = list.Count - 1; i >= 0; i--)
            {
                if (list[i].N > MaxCount / 2)
                {
                    var lastIndex = list.Count - 1;
                    if (i != lastIndex)
                    {
                        // 假设列表有值是 1 10 5 而当前 i = 1 而 lastIndex = 2 要移除元素 10 可以先将最后一个值赋值给当前的元素
                        // list[1] = list[lastIndex=2] = 5
                        // 赋值之后的列表是 1 5 5 也就是实际上干掉了 10 这个元素了
                        // 最后再删除多余的最后一个元素就可以了
                        list[i] = list[lastIndex];
                    }

                    list.RemoveAt(lastIndex);
                }
            }
        }

        public const int MaxCount = 20000000;

        public IEnumerable<List<Foo>> GetArgumentList()
        {
            var list = new List<Foo>(MaxCount);
            for (int i = 0; i < MaxCount / 2; i++)
            {
                list.Add(new Foo()
                {
                    N = i
                });
                list.Add(new Foo()
                {
                    N = MaxCount - i
                });
            }

            yield return list;
        }
    }

    public class Foo
    {
        public int N { set; get; }
    }
}
