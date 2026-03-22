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
            var list = new TrapList<T>();
            foreach (var item in source)
            {
                list.Add(item);
            }
            return list;
        }

        /// <summary>
        /// Converts an IEnumerable to a TrapHashSet.
        /// </summary>
        public static TrapHashSet<T> ToTrapHashSet<T>(this IEnumerable<T> source)
        {
            var set = new TrapHashSet<T>();
            foreach (var item in source)
            {
                set.Add(item);
            }
            return set;
        }

        /// <summary>
        /// Converts an IDictionary to a TrapDictionary.
        /// </summary>
        public static TrapDictionary<TKey, TValue> ToTrapDictionary<TKey, TValue>(this IDictionary<TKey, TValue> source)
            where TKey : notnull
        {
            var dict = new TrapDictionary<TKey, TValue>();
            foreach (var kvp in source)
            {
                dict.Add(kvp.Key, kvp.Value);
            }
            return dict;
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
