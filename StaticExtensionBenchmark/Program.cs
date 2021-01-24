using System;
using System.Collections.Generic;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace StaticExtensionBenchmark
{
    public static class Foo
    {
        public static string Property { get; } = "Property";

        public static string Field = "Field";
    }

    public class Program
    {
        static void Main(string[] args)
        {
            //var program = new Program();
            //program.GetFieldWithCache(new object[10]);

            BenchmarkRunner.Run<Program>();
        }

        /*
         * |                      Method | getObjectTimeList |      Mean |    Error |   StdDev |    Median | Ratio |
   |---------------------------- |------------------ |----------:|---------:|---------:|----------:|------:|
   |           GetFieldWithCache |      Object[1000] | 100.24 ns | 0.559 ns | 0.523 ns | 100.32 ns |  0.48 |
   |        GetPropertyWithCache |      Object[1000] | 245.21 ns | 0.784 ns | 0.733 ns | 245.13 ns |  1.18 |
   |    GetFieldWithOriginMethod |      Object[1000] |  69.04 ns | 0.008 ns | 0.008 ns |  69.03 ns |  0.33 |
   | GetPropertyWithOriginMethod |      Object[1000] | 207.04 ns | 1.895 ns | 1.772 ns | 207.85 ns |  1.00 |
   |           GetFieldWithCache |       Object[100] |  99.66 ns | 0.155 ns | 0.121 ns |  99.67 ns |  0.48 |
   |        GetPropertyWithCache |       Object[100] | 243.35 ns | 0.254 ns | 0.198 ns | 243.34 ns |  1.18 |
   |    GetFieldWithOriginMethod |       Object[100] |  71.19 ns | 0.793 ns | 0.742 ns |  71.76 ns |  0.34 |
   | GetPropertyWithOriginMethod |       Object[100] | 206.01 ns | 0.115 ns | 0.090 ns | 205.97 ns |  1.00 |
   |           GetFieldWithCache |        Object[10] |  99.67 ns | 0.263 ns | 0.246 ns |  99.59 ns |  0.48 |
   |        GetPropertyWithCache |        Object[10] | 247.12 ns | 0.731 ns | 0.611 ns | 246.92 ns |  1.19 |
   |    GetFieldWithOriginMethod |        Object[10] |  69.01 ns | 0.012 ns | 0.011 ns |  69.02 ns |  0.33 |
   | GetPropertyWithOriginMethod |        Object[10] | 206.21 ns | 0.202 ns | 0.169 ns | 206.11 ns |  1.00 |
   |           GetFieldWithCache |         Object[1] |  97.91 ns | 0.347 ns | 0.325 ns |  97.88 ns |  0.47 |
   |        GetPropertyWithCache |         Object[1] | 247.70 ns | 1.459 ns | 1.293 ns | 246.95 ns |  1.20 |
   |    GetFieldWithOriginMethod |         Object[1] |  69.21 ns | 0.268 ns | 0.237 ns |  69.02 ns |  0.33 |
   | GetPropertyWithOriginMethod |         Object[1] | 205.01 ns | 0.284 ns | 0.222 ns | 205.09 ns |  0.99 |
   |           GetFieldWithCache |         Object[2] |  97.61 ns | 0.210 ns | 0.197 ns |  97.66 ns |  0.47 |
   |        GetPropertyWithCache |         Object[2] | 248.89 ns | 1.166 ns | 1.091 ns | 249.21 ns |  1.20 |
   |    GetFieldWithOriginMethod |         Object[2] |  69.06 ns | 0.017 ns | 0.016 ns |  69.05 ns |  0.33 |
   | GetPropertyWithOriginMethod |         Object[2] | 206.72 ns | 0.054 ns | 0.042 ns | 206.71 ns |  1.00 |
         */

        [Benchmark()]
        [ArgumentsSource(nameof(GetTime))]
        public object GetFieldWithCache(object[] getObjectTimeList)
        {
            var creatorDictionary = new Dictionary<(Type type, string name), IFieldOrPropertyValueGetter>();
            for (var i = 0; i < getObjectTimeList.Length; i++)
            {
                GetFieldOrPropertyValueWithCache(typeof(Foo), "Field", out var value, creatorDictionary);

                getObjectTimeList[i] = value;
            }

            return getObjectTimeList;
        }

        [Benchmark()]
        [ArgumentsSource(nameof(GetTime))]
        public object GetPropertyWithCache(object[] getObjectTimeList)
        {
            var creatorDictionary = new Dictionary<(Type type, string name), IFieldOrPropertyValueGetter>();
            for (var i = 0; i < getObjectTimeList.Length; i++)
            {
                GetFieldOrPropertyValueWithCache(typeof(Foo), "Property", out var value, creatorDictionary);

                getObjectTimeList[i] = value;
            }

            return getObjectTimeList;
        }

        [Benchmark()]
        [ArgumentsSource(nameof(GetTime))]
        public object GetFieldWithOriginMethod(object[] getObjectTimeList)
        {
            for (var i = 0; i < getObjectTimeList.Length; i++)
            {
                GetFieldOrPropertyValue(typeof(Foo), "Field", out var value);

                getObjectTimeList[i] = value;
            }

            return getObjectTimeList;
        }

        [Benchmark(Baseline = true)]
        [ArgumentsSource(nameof(GetTime))]
        public object GetPropertyWithOriginMethod(object[] getObjectTimeList)
        {
            for (var i = 0; i < getObjectTimeList.Length; i++)
            {
                GetFieldOrPropertyValue(typeof(Foo), "Property", out var value);

                getObjectTimeList[i] = value;
            }

            return getObjectTimeList;
        }

        public IEnumerable<object[]> GetTime()
        {
            foreach (var count in GetTimeInner())
            {
                yield return new object[] {new object[count]};
            }

            IEnumerable<int> GetTimeInner()
            {
                yield return 1;
                yield return 2;
                yield return 10;
                yield return 100;
                yield return 1000;
            }
        }

        interface IFieldOrPropertyValueGetter
        {
            object GetObject();
        }

        class FieldValueGetter : IFieldOrPropertyValueGetter
        {
            public FieldValueGetter(FieldInfo fieldInfo)
            {
                _fieldInfo = fieldInfo;
            }

            public object GetObject()
            {
                return _fieldInfo.GetValue(null);
            }

            private readonly FieldInfo _fieldInfo;
        }

        class PropertyValueGetter : IFieldOrPropertyValueGetter
        {
            public PropertyValueGetter(PropertyInfo propertyInfo)
            {
                _propertyInfo = propertyInfo;
            }

            public object GetObject()
            {
                return _propertyInfo.GetValue(null, null);
            }

            private readonly PropertyInfo _propertyInfo;
        }

        private bool GetFieldOrPropertyValueWithCache(Type type, string name, out object value,
            Dictionary<(Type type, string name), IFieldOrPropertyValueGetter> creatorDictionary)
        {
            if (!creatorDictionary.TryGetValue((type, name), out var creator))
            {
                creator = GetCreator(type, name);
                creatorDictionary.Add((type, name), creator);
            }

            if (creator != null)
            {
                value = creator.GetObject();
                return true;
            }
            else
            {
                value = null;
                return false;
            }
        }

        private IFieldOrPropertyValueGetter GetCreator(Type type, string name)
        {
            FieldInfo field = null;
            Type temp = type;

            do
            {
                field = temp.GetField(name, BindingFlags.Public | BindingFlags.Static);
                if (field != null)
                {
                    return new FieldValueGetter(field);
                }

                temp = temp.BaseType;
            } while (temp != null);


            PropertyInfo prop = null;
            temp = type;

            do
            {
                prop = temp.GetProperty(name, BindingFlags.Public | BindingFlags.Static);
                if (prop != null)
                {
                    return new PropertyValueGetter(prop);
                }

                temp = temp.BaseType;
            } while (temp != null);

            return null;
        }

        private bool GetFieldOrPropertyValue(Type type, string name, out object value)
        {
            FieldInfo field = null;
            Type temp = type;

            do
            {
                field = temp.GetField(name, BindingFlags.Public | BindingFlags.Static);
                if (field != null)
                {
                    value = field.GetValue(null);
                    return true;
                }

                temp = temp.BaseType;
            } while (temp != null);


            PropertyInfo prop = null;
            temp = type;

            do
            {
                prop = temp.GetProperty(name, BindingFlags.Public | BindingFlags.Static);
                if (prop != null)
                {
                    value = prop.GetValue(null, null);
                    return true;
                }

                temp = temp.BaseType;
            } while (temp != null);

            value = null;
            return false;
        }
    }
}