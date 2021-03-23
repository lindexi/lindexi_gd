using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using ScrimpNet.Reflection;

namespace ScrimpNet
{
    public static partial class Extensions
    {
        /// <summary>
        /// Sort an IQuerable by single property in the source
        /// </summary>
        /// <typeparam name="T">Type of object in list that is being sorted</typeparam>
        /// <param name="source">List of objects to sort</param>
        /// <param name="propertyName">Single 'Property' name or 'PropertName DESC'</param>
        /// <returns>Sorted list</returns>
        /// <remarks>http://weblogs.asp.net/davidfowler/archive/2008/12/11/dynamic-sorting-with-linq.aspx</remarks>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> is null</exception>
        public static IQueryable<T> SortBy<T>(this IQueryable<T> source, string propertyName)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            if (String.IsNullOrEmpty(propertyName))
            {
                return source;
            }

            int descIndex = propertyName.ToUpper().IndexOf(" DESC");

            if (descIndex >= 0)
            {
                propertyName = propertyName.Substring(0, descIndex).Trim();
            }

            propertyName = propertyName.Replace(" ASC", "");


            ParameterExpression parameter = Expression.Parameter(source.ElementType, String.Empty);
            MemberExpression property = Expression.Property(parameter, propertyName);
            LambdaExpression lambda = Expression.Lambda(property, parameter);

            string methodName = (descIndex < 0) ? "OrderBy" : "OrderByDescending";
            Expression methodCallExpression = Expression.Call(typeof(Queryable), methodName,
                                new Type[] { source.ElementType, property.Type },
                                source.Expression, Expression.Quote(lambda));

            return source.Provider.CreateQuery<T>(methodCallExpression);
        }

        /// <summary>
        /// Allow a property name to be passed into LINQ Distinct() method
        /// </summary>
        /// <typeparam name="T">Type of element that is contained in list</typeparam>
        /// <param name="source">List of items to be filtered</param>
        /// <param name="propertyName">Name of property to use for comparision</param>
        /// <returns>List of distinct elements</returns>
        public static IQueryable<T> Distinct<T>(this IQueryable<T> source, string propertyName)
        {
            var comparer = new PropertyComparer<T>(propertyName);
            return source.Distinct(comparer);
        }

        /// <summary>
        /// Allow a property name to be passed into LINQ Distinct() method
        /// </summary>
        /// <typeparam name="T">Type of element that is contained in list</typeparam>
        /// <param name="source">List of items to be filtered</param>
        /// <param name="propertyName">Name of property to use for comparision</param>
        /// <returns>List of distinct elements</returns>
        public static IEnumerable<T> Distinct<T>(this IEnumerable<T> source, string propertyName)
        {
            var comparer = new PropertyComparer<T>(propertyName);
            return source.Distinct(comparer);
        }

        /// <summary>
        /// Allow a property name to be passed to LINQ Except() method 
        /// </summary>
        /// <typeparam name="T">Type of element that is contained in list</typeparam>
        /// <param name="source">List of items to be filtered</param>
        /// <param name="exclusionList">List of items to be excluded from <paramref name="source"/></param>
        /// <param name="propertyName">Name of property in elements to be used for comparison</param>
        /// <returns>List of elements in <paramref name="source"/> not found in <paramref name="exclusionList"/></returns>
        public static IEnumerable<T> Except<T>(this IEnumerable<T> source, IEnumerable<T> exclusionList, string propertyName)
        {
            var comparer = new PropertyComparer<T>(propertyName);
            return source.Except(exclusionList,comparer);
        }
    }
}
