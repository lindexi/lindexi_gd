using System;
using System.Collections.Generic;
using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace NalljalchuwabijelJallkalaiwela
{
    public class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<Program>();
        }

        [Benchmark]
        [ArgumentsSource(nameof(GetArguments))]
        public bool TestGreaterThanZero(double value)
        {
            return DoubleUtil.GreaterThanZero(value);
        }

        [Benchmark]
        [ArgumentsSource(nameof(GetArguments))]
        public bool TestGreaterThanZero2(double value)
        {
            return DoubleUtil.GreaterThanZero2(value);
        }

        [Benchmark]
        [ArgumentsSource(nameof(GetArguments))]
        public bool TestGreaterThan(double value)
        {
            return DoubleUtil.GreaterThan(value, 0);
        }

        public IEnumerable<double> GetArguments()
        {
            const int count = 10;
            for (int i = 0; i < count; i++)
            {
                yield return i;
            }

            for (double i = 0; i < count; i++)
            {
                yield return i / count;
            }

            for (int i = 0; i < count; i++)
            {
                yield return -i;
            }

            for (double i = 0; i < count; i++)
            {
                yield return -i / count;
            }
        }
    }

    public static class DoubleUtil
    {
        /// <summary>
        /// GreaterThanZero - Returns whether or not the value is greater than zero
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool GreaterThanZero2(double value)
        {
            // IsZero = Math.Abs(value) < 10.0 * DBL_EPSILON
            // value > 0 && !IsZero(value) == value > 0 && !(Math.Abs(value) < 10.0 * DBL_EPSILON)
            // = !(value < 10.0 * DBL_EPSILON)
            return value >= 10.0 * DBL_EPSILON;
        }

        public static bool GreaterThanZero(double value)
        {
            if (value > 0)
            {
                // AreClose(double value1, double value2)
                // = (|value1-value2| / (|value1| + |value2| + 10.0)) < DBL_EPSILON
                //
                // AreClose(value, 0) = (|value-0| / (|value| + 0 + 10.0)) < DBL_EPSILON
                // = value / (value + 10.0) < DBL_EPSILON
                double eps = (value + 10.0) * DBL_EPSILON;
                double delta = value;
                return !((-eps < delta) && (eps > delta));
            }
            else
            {
                return false;
            }
        }

        public static bool GreaterThan(double value1, double value2)
        {
            return (value1 > value2) && !AreClose(value1, value2);
        }

        public static bool IsZero(double value)
        {
            return Math.Abs(value) < 10.0 * DBL_EPSILON;
        }

        public static bool AreClose(double value1, double value2)
        {
            //in case they are Infinities (then epsilon check does not work)
            if (value1 == value2) return true;
            // This computes (|value1-value2| / (|value1| + |value2| + 10.0)) < DBL_EPSILON
            double eps = (Math.Abs(value1) + Math.Abs(value2) + 10.0) * DBL_EPSILON;
            double delta = value1 - value2;
            return (-eps < delta) && (eps > delta);
        }

        internal const double DBL_EPSILON = 2.2204460492503131e-016; /* smallest such that 1.0+DBL_EPSILON != 1.0 */
    }
}
