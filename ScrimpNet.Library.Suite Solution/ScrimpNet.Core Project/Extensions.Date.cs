using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ScrimpNet
{
    public static partial class Extensions
    {
        /// <summary>
        /// Create a date value that is safe to persist to MS-SQL
        /// </summary>
        /// <param name="dotNetDate">Value to be persisted</param>
        /// <returns>Date that has been truncated to SQLMin or SQLMax dates</returns>
        public static DateTime ToSqlDate(this DateTime dotNetDate)
        {
            return Utils.Date.ToSqlDate(dotNetDate);
        }
    }
}
