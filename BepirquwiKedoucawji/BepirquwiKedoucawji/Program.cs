using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace BepirquwiKedoucawji
{
    class Program
    {
        static void Main(string[] args)
        {
            if (Enum.TryParse(typeof(Di),null,out var value))
            {
                
            }
        }
    }

    public enum Di
    {
        /// <summary>
        /// 轨道
        /// </summary>
        Railway,

        /// <summary>
        /// 河流
        /// </summary>
        River,
    }
}
