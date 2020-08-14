using System.Collections.Generic;
using BenchmarkDotNet.Attributes;

namespace HearwhejiyehallyiheFubaduwheefu
{
    public class RegistryGetBaseKeyFromKeyNameBenchmark
    {
        [Benchmark(Baseline = true, OperationsPerInvoke = 1000)]
        [ArgumentsSource(nameof(GetKeyNameList))]
        public object OldGetBaseKeyFromKeyName(string keyName)
        {
            return Registry.OldGetBaseKeyFromKeyName(keyName, out _);
        }

        [Benchmark(OperationsPerInvoke = 1000)]
        [ArgumentsSource(nameof(GetKeyNameList))]
        public object NewGetBaseKeyFromKeyName(string keyName)
        {
            return Registry.NewGetBaseKeyFromKeyName(keyName, out _);
        }

        public IEnumerable<object[]> GetKeyNameList()
        {
            yield return new object[] { "" };
            yield return new object[] { "Error" };
            yield return new object[] { @"Error\123" };
            yield return new object[] { @"Error\12345" };

            yield return new object[] { @"HKEY_CURRENT_USER" };
            yield return new object[] { @"HKEY_LOCAL_MACHINE" };
            yield return new object[] { @"HKEY_CLASSES_ROOT" };
            yield return new object[] { @"HKEY_PERFORMANCE_DATA" };
            yield return new object[] { @"HKEY_CURRENT_CONFIG" };

            yield return new object[] { @"HKEY\CURRENT_USER" };
            yield return new object[] { @"HKEY\LOCAL_MACHINE" };
            yield return new object[] { @"HKEY\CLASSES_ROOT" };
            yield return new object[] { @"HKEY\PERFORMANCE_DATA" };
            yield return new object[] { @"HKEY\CURRENT_CONFIG" };

            yield return new object[] { @"HKEY_CURRENT_USER\subKeyName" };
            yield return new object[] { @"HKEY_LOCAL_MACHINE\subKeyName" };
            yield return new object[] { @"HKEY_CLASSES_ROOT\subKeyName" };
            yield return new object[] { @"HKEY_PERFORMANCE_DATA\subKeyName" };
            yield return new object[] { @"HKEY_CURRENT_CONFIG\subKeyName" };
        }
    }
}