using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Walterlv.Demo.XamlProperties
{
    public static class DependencyTest
    {
        public static readonly System.Windows.DependencyProperty FooProperty = System.Windows.DependencyProperty.RegisterAttached(
            "Foo", typeof(bool), typeof(DependencyTest),
            new System.Windows.PropertyMetadata(default(bool)));

        public static bool GetFoo(System.Windows.DependencyObject element) => (bool)element.GetValue(FooProperty);

        public static void SetFoo(System.Windows.DependencyObject element, bool value) => element.SetValue(FooProperty, value);
    }
}
