using System;

namespace HearwhejiyehallyiheFubaduwheefu
{
    public sealed partial class RegistryKey : MarshalByRefObject
    {
        public RegistryKey(string name)
        {
            Name = name;
        }

        public string Name { get; set; }
    }
}