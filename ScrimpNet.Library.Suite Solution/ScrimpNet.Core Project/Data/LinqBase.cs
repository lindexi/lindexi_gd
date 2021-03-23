using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace ScrimpNet.Data
{
    /// <summary>
    /// Remove an entity from it's data context
    /// </summary>
    [DataContract]
    public abstract class LinqBase : IDetachable
    {
        /// <summary>
        /// Force a call to the Linq Entity's 'Initialize' method which effectively breaks the binding to data context
        /// </summary>
        [Citation("http://www.jstawski.com/archive/2008/07/09/linq-to-sql-generic-detach.aspx", Author = "Jonas Stawski")]
        public virtual void Detach()
        {
            if (IsAttached == true)
            {
                GetType().GetMethod("Initialize", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(this, null);
             
            }
        }
        /// <summary>
        /// Returns TRUE if Linq entity is bound to a data context
        /// </summary>
        public virtual bool IsAttached
        {
            get
            {
               //return (PropertyChanged.GetInvocationList().Length > 0);
                return false;
            }

        }


        //public abstract event PropertyChangingEventHandler PropertyChanging;

        //public abstract event PropertyChangedEventHandler PropertyChanged;
    }
}
