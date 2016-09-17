namespace NewtonVR
{
    using System;
    static class ArrayExtensions
    {
        public static TB[] Map<TA, TB>(this TA[] collection, Func<TA, TB> convert)
        {
            var converted = new TB[collection.Length];

            for (var i = 0; i < converted.Length; i++)
                converted[i] = convert(collection[i]);

            return converted;
        }
    }
}