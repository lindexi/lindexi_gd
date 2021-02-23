using System;
using System.Collections.Generic;
using System.Linq;

namespace 分型
{
    class Graph
    {
        public void AddPoint(IPoint point,
            IPoint[] inputPointList,
            IPoint[] outputPointList)
        {
            if (!ReferenceEquals(point.Graph, this))
            {
                throw new ArgumentException();
            }

            InputPointDictionary[point] = inputPointList.ToList();
            OutputPointDictionary[point] = outputPointList.ToList();

            foreach (var temp in inputPointList)
            {
                // 自己的输入就是对方的输出
                AddPoint(OutputPointDictionary, temp, point);
            }

            foreach (var temp in outputPointList)
            {
                AddPoint(InputPointDictionary, temp, point);
            }
        }

        private static void AddPoint(Dictionary<IPoint, List<IPoint>> dictionary, IPoint key, IPoint value)
        {
            if (dictionary.TryGetValue(key,
                   out var list))
            {
                if (!list.Contains(value))
                {
                    list.Add(value);
                }
            }
            else
            {
                dictionary[key] = new List<IPoint>()
                    {
                        value
                    };
            }
        }

        public void RemovePoint(IPoint point)
        {
            if (InputPointDictionary.Remove(point, out var inputList))
            {
                // 自己的输入就是对方的输出
                RemoveDictionaryPoint(OutputPointDictionary, inputList, point);
            }

            if(OutputPointDictionary.Remove(point,out var outputList))
            {
                RemoveDictionaryPoint(InputPointDictionary, outputList, point);
            }
        }

        private static void RemoveDictionaryPoint(Dictionary<IPoint, List<IPoint>> dictionary,
            List<IPoint> keyList,IPoint removePoint)
        {
            foreach (var key in keyList)
            {
                RemoveDictionaryPoint(dictionary, key, removePoint);
            }
        }

        private static void RemoveDictionaryPoint(Dictionary<IPoint, List<IPoint>> dictionary,
         IPoint key, IPoint removePoint)
        {
            if(dictionary.TryGetValue(key,out var list))
            {
                list.Remove(removePoint);
            }
        }

        public IPoint[] GetInputPointList(IPoint point)
        {
            return InputPointDictionary[point].ToArray();
        }

        public IPoint[] GetOutputPointList(IPoint point)
        {
            return OutputPointDictionary[point].ToArray();
        }

        private Dictionary<IPoint, List<IPoint>> InputPointDictionary { get; }
        = new();

        private Dictionary<IPoint, List<IPoint>> OutputPointDictionary { get; }
        = new();
    }
}
