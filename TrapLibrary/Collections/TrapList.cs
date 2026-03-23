using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Debugging.Traps
{
    public class TrapList<T> : Collection<T>
    {
        // Thread lock to prevent conflicts when configuring or triggering Traps in multi-threaded environments
        private readonly object _trapLock = new object();
        // Optimized bucket storage: fast lookups, no allocations during execution
        private readonly Dictionary<TrapEventType, ListTrapRule<T>[]> _ruleBuckets;

        private Dictionary<TrapEventType, ListTrapRule<T>[]> InitializeBuckets()
        {
            var dict = new Dictionary<TrapEventType, ListTrapRule<T>[]>();
            foreach (TrapEventType type in Enum.GetValues(typeof(TrapEventType)))
            {
                dict[type] = new ListTrapRule<T>[0];
            }
            return dict;
        }

        public TrapList() 
        {
            _ruleBuckets = InitializeBuckets();
        }
        
        public TrapList(IEnumerable<T> collection) : base(collection.ToList()) 
        {
            _ruleBuckets = InitializeBuckets();
        }

        // --- Fluent API Entry Points ---

        public ListTrapBuilder<T> OnAdd() => new ListTrapBuilder<T>(this, TrapEventType.Added);
        public ListTrapBuilder<T> OnRemove() => new ListTrapBuilder<T>(this, TrapEventType.Removed);
        public ListTrapBuilder<T> OnUpdate() => new ListTrapBuilder<T>(this, TrapEventType.Updated);
        public ListTrapBuilder<T> OnClear() => new ListTrapBuilder<T>(this, TrapEventType.Cleared);

        internal void AddRule(ListTrapRule<T> rule)
        {
            lock (_trapLock)
            {
                // Copy-On-Write: Create new array, copy old, add new, replace reference
                // This allows lock-free reads in ExecuteTraps
                var existing = _ruleBuckets[rule.EventType];
                var newArray = new ListTrapRule<T>[existing.Length + 1];
                Array.Copy(existing, newArray, existing.Length);
                newArray[existing.Length] = rule;
                _ruleBuckets[rule.EventType] = newArray;
            }
        }

        // --- Direct Access Methods (Bypass Traps) ---

        public void AddWithoutTrap(T item)
        {
            Items.Add(item);
        }

        public void AddRange(IEnumerable<T> collection)
        {
            foreach (var item in collection)
            {
                Items.Add(item);
            }
        }

        // --- Core Interception Logic (Override Protected Methods) ---

        protected override void InsertItem(int index, T item)
        {
            ExecuteTraps(TrapEventType.Added, item);
            base.InsertItem(index, item);
        }

        protected override void SetItem(int index, T item)
        {
            // Get old value for potential comparison logic extension
            T oldItem = this[index]; 
            ExecuteTraps(TrapEventType.Updated, item, oldItem);
            base.SetItem(index, item);
        }

        protected override void RemoveItem(int index)
        {
            T removedItem = this[index];
            ExecuteTraps(TrapEventType.Removed, removedItem);
            base.RemoveItem(index);
        }

        protected override void ClearItems()
        {
            ExecuteTraps(TrapEventType.Cleared, default!);
            base.ClearItems();
        }

        // --- Execution Engine ---

        private void ExecuteTraps(TrapEventType eventType, T item, T? oldItem = default)
        {
            if (!TrapManager.Enabled) return;

            // Direct bucket access: O(1) lookup, no LINQ, no allocation, no lock needed for reading
            var rules = _ruleBuckets[eventType];
            if (rules.Length == 0) return;

            for (int i = 0; i < rules.Length; i++)
            {
                var rule = rules[i];
                try
                {
                    // Execute Action only if Predicate is satisfied
                    if (rule.Predicate == null || rule.Predicate(item))
                    {
                        rule.Action?.Invoke();
                    }
                }
                catch (Exception ex)
                {
                    try { Console.Error.WriteLine($"[CollectionSpy Error] Trap failed: {ex}"); } catch {}
                }
            }
        }
    }

    // --- List Related Builder and Rule ---

    public class ListTrapRule<T>
    {
        public TrapEventType EventType { get; set; }
        public Func<T, bool>? Predicate { get; set; }
        public Action? Action { get; set; }
    }

    public class ListTrapBuilder<T>
    {
        private readonly TrapList<T> _list;
        private readonly TrapEventType _type;
        private Func<T, bool>? _predicate;

        public ListTrapBuilder(TrapList<T> list, TrapEventType type)
        {
            _list = list;
            _type = type;
        }

        public ListTrapBuilder<T> When(Func<T, bool> predicate)
        {
            _predicate = predicate;
            return this;
        }

        public void Do(Action action)
        {
            var rule = new ListTrapRule<T>
            {
                EventType = _type,
                Predicate = _predicate, // If null, triggers unconditionally
                Action = action
            };
            _list.AddRule(rule);
        }
    }
}
