using System;
using System.Windows;
using Microsoft.Xaml.Behaviors;

namespace JeyaijikeneeWhejoniwairbu
{
    public class ResourceChangeEventBehavior : Behavior<FrameworkElement>
    {
        public static readonly DependencyProperty ResourceProperty = DependencyProperty.Register(
            "Resource", typeof(object), typeof(ResourceChangeEventBehavior), new PropertyMetadata(default(object), ResourceChangedCallback));

        public event EventHandler<ResourceChangedEventArgs> ResourceChanged;

        public object Resource
        {
            get { return GetValue(ResourceProperty); }
            set { SetValue(ResourceProperty, value); }
        }

        private static void ResourceChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
        {
            if (dependencyObject is ResourceChangeEventBehavior resourceChangeNotifier)
            {
                resourceChangeNotifier.OnResourceChanged(new ResourceChangedEventArgs(args.OldValue, args.NewValue));
            }
        }

        private void OnResourceChanged(ResourceChangedEventArgs args)
        {
            ResourceChanged?.Invoke(this, args);
        }
    }
}