using System.Collections.Generic;
using System.Linq;
using Debugging.Traps;

namespace Debugging.Traps.Extensions
{
    public static class CollectionSpyExtensions
    {
        /// <summary>
        /// Converts an IEnumerable to a TrapList.
        /// </summary>
        public static TrapList<T> ToTrapList<T>(this IEnumerable<T> source)
        {
            return new TrapList<T>(source);
        }

        /// <summary>
        /// Converts an IEnumerable to a TrapHashSet.
        /// </summary>
        public static TrapHashSet<T> ToTrapHashSet<T>(this IEnumerable<T> source)
        {
            return new TrapHashSet<T>(source);
        }

        /// <summary>
        /// Converts an IDictionary to a TrapDictionary.
        /// </summary>
        public static TrapDictionary<TKey, TValue> ToTrapDictionary<TKey, TValue>(this IDictionary<TKey, TValue> source)
            where TKey : notnull
        {
            return new TrapDictionary<TKey, TValue>(source);
        }

        /// <summary>
        /// Converts an IEnumerable to a TrapQueue.
        /// </summary>
        public static TrapQueue<T> ToTrapQueue<T>(this IEnumerable<T> source)
        {
            return new TrapQueue<T>(source);
        }

        /// <summary>
        /// Converts an IEnumerable to a TrapStack.
        /// </summary>
        public static TrapStack<T> ToTrapStack<T>(this IEnumerable<T> source)
        {
            return new TrapStack<T>(source);
        }
    }
}
