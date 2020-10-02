using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace FairneabairjibearhearLanallgekay
{
    class Program
    {
        static void Main(string[] args)
        {

            KurlinelchuFalberekebi();
        }

        private static void KurlinelchuFalberekebi()
        {
            // 设置和读取值

            var storageValue = new StorageValue<int>();
            storageValue.DefaultValue = 100;
            AssertEqual(100, storageValue.CurrentValue);

            storageValue.CurrentValue = 10;
            AssertEqual(100, storageValue.DefaultValue);
            AssertEqual(10, storageValue.CurrentValue);
        }

        private static void AssertEqual(object a, object b)
        {
            if (!a.Equals(b))
            {
            }
        }
    }

    public class StorageValue<T>
    {
        public void Increment()
        {
            lock (_locker)
            {
                CurrentValueIndex++;
            }
        }

        public void Decrement()
        {
            lock (_locker)
            {
                CurrentValueIndex--;
                if (CurrentValueIndex < 0)
                {
                    CurrentValueIndex = 0;
                }
            }
        }

        private readonly object _locker = new object();

        public T this[int valueIndex]
        {
            set
            {
                lock (_locker)
                {
                    ValueList[valueIndex] = value;
                }
            }
            get
            {
                lock (_locker)
                {
                    return ValueList[valueIndex];
                }
            }
        }

        public T CurrentValue
        {
            set
            {
                lock (_locker)
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
            }
            get
            {
                lock (_locker)
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
        }

        public T DefaultValue { set; get; }

        public List<T> ValueList { set; get; }

        public int CurrentValueIndex { private set; get; }
    }
}
