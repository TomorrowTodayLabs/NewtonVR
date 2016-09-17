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

        public static IList<TB> Map<TA, TB>(this IList<TA> collection, Func<TA, TB> convert)
        {
            var converted = new TB[collection.Count];

            for (var i = 0; i < converted.Length; i++)
                converted[i] = convert(collection[i]);

            return converted;
        }

        public static T Reduce<T>(this IList<T> collection, Func<T, T, T> accumulator, T start)
        {
            var reduction = start;

            collection.Iterate(element => reduction = accumulator(reduction, element));

            return reduction;
        }
    }
}