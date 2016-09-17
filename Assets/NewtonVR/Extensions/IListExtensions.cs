namespace NewtonVR
{
    using System;
    using System.Collections.Generic;
    static class IListExtensions
    {
        public static void Iterate<T>(this IList<T> collection, Action<T> action)
        {
            for (var i = 0; i < collection.Count; i++)
                action(collection[i]);
        }

        public static T Reduce<T>(this IList<T> collection, Func<T, T, T> monoid, T start)
        {
            var reduction = start;

            collection.Iterate(element => monoid(reduction, element));

            return reduction;
        }
    }
}