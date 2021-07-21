using System;

namespace JeyaijikeneeWhejoniwairbu
{
    public class ResourceChangedEventArgs : EventArgs
    {
        public ResourceChangedEventArgs(object oldValue, object newValue)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }

        public object OldValue { get; }
        public object NewValue { get; }
    }
}