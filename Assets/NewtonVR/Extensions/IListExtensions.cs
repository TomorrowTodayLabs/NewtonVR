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
    }
}