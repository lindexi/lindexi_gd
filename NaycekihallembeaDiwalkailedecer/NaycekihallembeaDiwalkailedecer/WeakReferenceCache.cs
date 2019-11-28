using System;
using System.Collections.Generic;

namespace NaycekihallembeaDiwalkailedecer
{
    /// <summary>
    /// 弱引用缓存
    /// </summary>
    public class WeakReferenceCache
    {
        /// <summary>
        /// 从缓存获取或在没有获取到创建
        /// </summary>
        public T GetOrCreate<T>(object key, Func<T> createFunc)
        {
            if (_cacheList.TryGetValue(key, out var weakReference))
            {
                if (weakReference.TryGetTarget(out var value))
                {
                    return (T) value;
                }
            }

            var t = createFunc();
            weakReference = new WeakReference<object>(t);
            _cacheList[key] = weakReference;
            return t;
        }

        /// <summary>
        /// 从缓存获取或在没有获取到创建
        /// </summary>
        public T GetOrCreate<T>(Func<T> createFunc)
        {
            var type = typeof(T);
            return GetOrCreate(type, createFunc);
        }

        private readonly Dictionary<object, WeakReference<object>> _cacheList =
            new Dictionary<object, WeakReference<object>>();
    }
}