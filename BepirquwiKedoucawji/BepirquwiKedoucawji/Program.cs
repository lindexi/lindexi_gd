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
            List<string> errors = new List<string>();

            List<DateTime> list = JsonConvert.DeserializeObject<List<DateTime>>(@"[
      '2009-09-09T00:00:00Z',
      '这是歪楼的',
      [
        1
      ],
      '1977-02-20T00:00:00Z',
      null,
      '2000-12-01T00:00:00Z'
    ]",
                new JsonSerializerSettings
                {
                    Error = (sender, e) =>
                    {
                        errors.Add(e.ErrorContext.Error.Message);
                        e.ErrorContext.Handled = true;
                    },
                    Converters = { new IsoDateTimeConverter() }
                });
        }
    }
}
