using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Debugging.Traps
{
    public class TrapDictionary<TKey, TValue> : IDictionary<TKey, TValue> where TKey : notnull
    {
        private readonly Dictionary<TKey, TValue> _inner = new Dictionary<TKey, TValue>();
        private readonly object _trapLock = new object();
        private readonly List<DictTrapRule<TKey, TValue>> _rules = new List<DictTrapRule<TKey, TValue>>();

        // --- Fluent API Entry Points ---

        public DictTrapBuilder<TKey, TValue> OnAdd() => new DictTrapBuilder<TKey, TValue>(this, TrapEventType.Added);
        public DictTrapBuilder<TKey, TValue> OnRemove() => new DictTrapBuilder<TKey, TValue>(this, TrapEventType.Removed);
        public DictTrapBuilder<TKey, TValue> OnUpdate() => new DictTrapBuilder<TKey, TValue>(this, TrapEventType.Updated);
        public DictTrapBuilder<TKey, TValue> OnClear() => new DictTrapBuilder<TKey, TValue>(this, TrapEventType.Cleared);

        internal void AddRule(DictTrapRule<TKey, TValue> rule)
        {
            lock (_trapLock)
            {
                _rules.Add(rule);
            }
        }

        // --- Core Interception Logic (IDictionary Implementation) ---

        public TValue this[TKey key]
        {
            get => _inner[key];
            set
            {
                if (_inner.ContainsKey(key))
                {
                    ExecuteTraps(TrapEventType.Updated, key, value, _inner[key]);
                }
                else
                {
                    ExecuteTraps(TrapEventType.Added, key, value);
                }
                _inner[key] = value;
            }
        }

        public void Add(TKey key, TValue value)
        {
            ExecuteTraps(TrapEventType.Added, key, value);
            _inner.Add(key, value);
        }

        public bool Remove(TKey key)
        {
            if (_inner.TryGetValue(key, out var value))
            {
                // Trigger before removal to see context
                ExecuteTraps(TrapEventType.Removed, key, value);
                return _inner.Remove(key);
            }
            return false;
        }

        public void Clear()
        {
            ExecuteTraps(TrapEventType.Cleared, default!, default!);
            _inner.Clear();
        }

        // Explicit interface implementation to handle Add(KeyValuePair) and Remove(KeyValuePair)
        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        {
             if (_inner.TryGetValue(item.Key, out var val) && EqualityComparer<TValue>.Default.Equals(val, item.Value))
             {
                 ExecuteTraps(TrapEventType.Removed, item.Key, item.Value!);
                 return _inner.Remove(item.Key);
             }
             return false;
        }

        // --- Pass-through Methods ---
        public ICollection<TKey> Keys => _inner.Keys;
        public ICollection<TValue> Values => _inner.Values;
        public int Count => _inner.Count;
        public bool IsReadOnly => false;
        public bool ContainsKey(TKey key) => _inner.ContainsKey(key);
        public bool TryGetValue(TKey key, out TValue value) => _inner.TryGetValue(key, out value);
        public bool Contains(KeyValuePair<TKey, TValue> item) => ((ICollection<KeyValuePair<TKey, TValue>>)_inner).Contains(item);
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => ((ICollection<KeyValuePair<TKey, TValue>>)_inner).CopyTo(array, arrayIndex);
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _inner.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _inner.GetEnumerator();


        // --- Execution Engine ---

        private void ExecuteTraps(TrapEventType eventType, TKey key, TValue value, TValue? oldValue = default!)
        {
            if (!TrapManager.Enabled || _rules.Count == 0) return;

            List<DictTrapRule<TKey, TValue>> activeRules;
            lock (_trapLock)
            {
                activeRules = _rules.Where(r => r.EventType == eventType).ToList();
            }

            foreach (var rule in activeRules)
            {
                try
                {
                    // Dictionary Predicate receives Key and Value
                    if (rule.Predicate == null || rule.Predicate(key, value))
                    {
                        rule.Action?.Invoke();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[TrapDictionary Error] {ex.Message}");
                }
            }
        }
    }

    // --- Dictionary Related Builder and Rule ---

    public class DictTrapRule<TKey, TValue> where TKey : notnull
    {
        public TrapEventType EventType { get; set; }
        public Func<TKey, TValue, bool>? Predicate { get; set; } // Dictionary needs to check both Key and Value
        public Action? Action { get; set; }
    }

    public class DictTrapBuilder<TKey, TValue> where TKey : notnull
    {
        private readonly TrapDictionary<TKey, TValue> _dict;
        private readonly TrapEventType _type;
        private Func<TKey, TValue, bool>? _predicate;

        public DictTrapBuilder(TrapDictionary<TKey, TValue> dict, TrapEventType type)
        {
            _dict = dict;
            _type = type;
        }

        // Overload: Care only about Value
        public DictTrapBuilder<TKey, TValue> WhenValue(Func<TValue, bool> valuePredicate)
        {
            _predicate = (k, v) => valuePredicate(v);
            return this;
        }

        // Overload: Care only about Key
        public DictTrapBuilder<TKey, TValue> WhenKey(Func<TKey, bool> keyPredicate)
        {
            _predicate = (k, v) => keyPredicate(k);
            return this;
        }

        // Overload: Care about both Key and Value
        public DictTrapBuilder<TKey, TValue> When(Func<TKey, TValue, bool> predicate)
        {
            _predicate = predicate;
            return this;
        }

        public void Do(Action action)
        {
            var rule = new DictTrapRule<TKey, TValue>
            {
                EventType = _type,
                Predicate = _predicate,
                Action = action
            };
            _dict.AddRule(rule);
        }
    }
}
