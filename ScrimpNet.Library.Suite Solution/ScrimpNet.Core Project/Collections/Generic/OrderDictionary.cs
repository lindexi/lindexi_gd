using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Specialized;

namespace ScrimpNet.Collections.Generic
{
    public class OrderedDictionary<T> : OrderedDictionary where T:class
    {
        public T this[int index]
        {
            get
            {
                return base[index] as T;
            }
            set
            {
                base[index] = value;
            }
        }

        public T this[string key]
        {
            get
            {
                return (T)base[key];
            }
            set
            {
                base[key] = value;
            }
        }

    }
}
