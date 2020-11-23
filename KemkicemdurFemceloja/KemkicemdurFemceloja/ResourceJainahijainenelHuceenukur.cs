using System.Diagnostics;
using System.Windows;

namespace KemkicemdurFemceloja
{
    public class ResourceJainahijainenelHuceenukur : ResourceDictionary
    {
        public ResourceJainahijainenelHuceenukur()
        {
            Debugger.Break();
        }

        protected override void OnGettingValue(object key, ref object value, out bool canCache)
        {
            Debugger.Break();
            base.OnGettingValue(key, ref value, out canCache);
        }
    }
}