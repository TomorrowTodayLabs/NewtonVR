namespace NewtonVR
{
    using System;
    static class NullableExtensions
    {
        public static void Map<T>(this T? option, Action<T> action) where T : struct
        {
            if (option.HasValue)
                action(option.Value);
        }
    }
}