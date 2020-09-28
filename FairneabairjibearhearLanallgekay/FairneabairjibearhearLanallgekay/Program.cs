using System;
using System.Collections.Generic;
using System.Linq;

namespace FairneabairjibearhearLanallgekay
{
    class Program
    {
        static void Main(string[] args)
        {
            List<int> l=new List<int>();
            l.Capacity = 10;
            Console.WriteLine(l[5]);
        }
    }

    public class StorageValue<T>
    {
        public T this[int valueIndex]
        {
            set
            {
                ValueList[valueIndex] = value;
            }
            get => ValueList[valueIndex];
        }

        public T CurrentValue
        {
            set
            {
                ValueList ??= new List<T>();

                if (CurrentValueIndex == ValueList.Count)
                {
                    ValueList.Add(value);
                }
                else if (CurrentValueIndex > ValueList.Count)
                {
                    var addCount = CurrentValueIndex - ValueList.Count;
                    ValueList.AddRange(Enumerable.Repeat<T>(default, addCount));

                    ValueList.Add(value);
                }
                else
                {
                    ValueList[CurrentValueIndex] = value;
                }
            }
            get
            {
                if (ValueList == null || ValueList.Count == 0)
                {
                    return DefaultValue;
                }

                if (CurrentValueIndex >= ValueList.Count)
                {
                    return ValueList[^1];
                }

                return ValueList[CurrentValueIndex];
            }
        }

        public T DefaultValue { set; get; }

        public List<T> ValueList { set; get; }

        public int CurrentValueIndex { set; get; }
    }
}
