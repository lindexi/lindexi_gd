using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

// ReSharper disable All

namespace BepirquwiKedoucawji
{
    class Program
    {
        static void Main(string[] args)
        {
            var type = typeof(F1);

            foreach (var typeCustomAttribute in type.CustomAttributes)
            {
                
            }
        }
    }

    [Foo(kuqairjeabayjairnearKokaneberelefo: "123", ReejajurwhohallRahekaiqaw = "12")]
    public class F1
    {
    }

    class Foo : Attribute
    {
        /// <inheritdoc />
        public Foo(string kuqairjeabayjairnearKokaneberelefo)
        {
            KuqairjeabayjairnearKokaneberelefo = kuqairjeabayjairnearKokaneberelefo;
        }

        public string KuqairjeabayjairnearKokaneberelefo { get; set; }

        public string ReejajurwhohallRahekaiqaw { get; set; } = "123";
        public string HeenejallqaliFaqeeleha { get; set; } = "123";
    }

    static class Extension
    {
        public static void Deconstruct(this List<int> list, out string name, out int count)
        {
            name = string.Join(",", list);
            count = list.Count;
        }
    }
}