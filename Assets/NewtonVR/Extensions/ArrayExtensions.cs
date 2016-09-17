namespace NewtonVR
{
    using System;
    using System.Collections.Generic;;
    static class ArrayExtensions
    {
        public static IList<TB> Map<TA, TB>(this IList<TA> collection, Func<TA, TB> convert)
        {
            var converted = new TB[collection.Count];

            for (var i = 0; i < converted.Length; i++)
                converted[i] = convert(collection[i]);

            return converted;
        }
    }
}