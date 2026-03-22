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
        private readonly List<ListTrapRule<T>> _rules = new List<ListTrapRule<T>>();

        // --- Fluent API Entry Points ---

        public ListTrapBuilder<T> OnAdd() => new ListTrapBuilder<T>(this, TrapEventType.Added);
        public ListTrapBuilder<T> OnRemove() => new ListTrapBuilder<T>(this, TrapEventType.Removed);
        public ListTrapBuilder<T> OnUpdate() => new ListTrapBuilder<T>(this, TrapEventType.Updated);
        public ListTrapBuilder<T> OnClear() => new ListTrapBuilder<T>(this, TrapEventType.Cleared);

        internal void AddRule(ListTrapRule<T> rule)
        {
            lock (_trapLock)
            {
                _rules.Add(rule);
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
            List<ListTrapRule<T>> activeRules;
            lock (_trapLock)
            {
                // Snapshot to avoid modification during enumeration
                activeRules = _rules.Where(r => r.EventType == eventType).ToList();
            }

            foreach (var rule in activeRules)
            {
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
                    // Suppress exceptions to prevent debug code from crashing main application
                    Console.WriteLine($"[TrapList Error] Trap execution failed: {ex.Message}");
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
