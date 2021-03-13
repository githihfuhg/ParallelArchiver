using System.Collections.Generic;
using System.Linq;

namespace ArrArchiverLib.Extensions
{
    public static class IEnumerableExtensions
    {
        public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> items, int maxItems)
        {
            return items
                .Select((x, y) => new { Index = y, Value = x })
                .GroupBy(x => x.Index / maxItems)
                .Select(x => x.Select(y => y.Value));
        }
    }
}