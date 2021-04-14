using System;
using System.Collections.Generic;
using System.Linq;

namespace Mochizuki.VRChat.ParticleLiveToolkit.Internal
{
    internal static class EnumerableExtensions
    {
        public static IEnumerable<string> Duplicate<T>(this IEnumerable<T> obj, Func<T, string> groupByFunc)
        {
            return obj.GroupBy(groupByFunc).Where(w => w.Count() > 1).Select(w => w.Key).ToList();
        }
    }
}