using System.Collections.Generic;
using Windows.UI.Xaml.Markup;

namespace LocerjanayJarberlewerfair
{
    [MarkupExtensionReturnType(ReturnType = typeof(string))]
    public class LangExtension : MarkupExtension
    {
        public string Key { get; set; }

        protected override object ProvideValue()
        {
            if (LangList.TryGetValue(Key, out var value))
            {
                return value;
            }

            return Key;
        }

        public static Dictionary<string, string> LangList { set; get; } = new Dictionary<string, string>();
    }
}