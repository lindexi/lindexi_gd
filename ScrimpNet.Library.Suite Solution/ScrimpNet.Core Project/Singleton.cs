using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace ScrimpNet
{
    /// <summary>
    /// Create a singleton instance of any newable class
    /// </summary>
    /// <typeparam name="T">Class type that is being created as a singleton</typeparam>
    [Citation("http://www.codeproject.com/KB/architecture/GenericSingletonPattern.aspx#")]
    public static class Singleton<T>
           where T : class
    {
        static volatile T _instance;
        static object _lock = new object();

        static Singleton()
        {
        }

        /// <summary>
        /// Create/get a single instance of a class
        /// </summary>
        public static T Instance
        {
            get
            {
                if (_instance == null)
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            ConstructorInfo constructor = null;

                            try
                            {
                                // Binding flags exclude public constructors.
                                constructor = typeof(T).GetConstructor(BindingFlags.Instance |
                                              BindingFlags.NonPublic, null, new Type[0], null);
                            }
                            catch (Exception exception)
                            {
                                throw new InvalidOperationException(exception.Message,exception);
                            }

                            if (constructor == null || constructor.IsAssembly)
                                // Also exclude internal constructors.
                                throw new InvalidOperationException(string.Format("A private or " +
                                      "protected constructor is missing for '{0}'.", typeof(T).Name));

                            _instance = (T)constructor.Invoke(null);
                        }
                    }

                return _instance;
            }
        }
    }
}
