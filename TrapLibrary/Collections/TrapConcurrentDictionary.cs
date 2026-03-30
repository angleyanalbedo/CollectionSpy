using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Debugging.Traps
{
    /// <summary>
    /// A thread-safe dictionary that supports trap interception.
    /// Wraps a ConcurrentDictionary<TKey, TValue> to provide lock-free reads and thread-safe writes.
    /// </summary>
    public class TrapConcurrentDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>, ITrapDictionaryTarget<TKey, TValue> where TKey : notnull
    {
        private readonly ConcurrentDictionary<TKey, TValue> _inner;
        private readonly object _trapLock = new object();
        private readonly Dictionary<TrapEventType, DictTrapRule<TKey, TValue>[]> _ruleBuckets;

        public TrapConcurrentDictionary() 
        {
            _inner = new ConcurrentDictionary<TKey, TValue>();
            _ruleBuckets = InitializeBuckets();
        }

        public TrapConcurrentDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection)
        {
            _inner = new ConcurrentDictionary<TKey, TValue>(collection);
            _ruleBuckets = InitializeBuckets();
        }
        
        public TrapConcurrentDictionary(IEqualityComparer<TKey> comparer)
        {
            _inner = new ConcurrentDictionary<TKey, TValue>(comparer);
            _ruleBuckets = InitializeBuckets();
        }

        private Dictionary<TrapEventType, DictTrapRule<TKey, TValue>[]> InitializeBuckets()
        {
            var dict = new Dictionary<TrapEventType, DictTrapRule<TKey, TValue>[]>();
            foreach (TrapEventType type in Enum.GetValues(typeof(TrapEventType)))
            {
                dict[type] = new DictTrapRule<TKey, TValue>[0];
            }
            return dict;
        }

        // --- Fluent API Entry Points ---

        public DictTrapBuilder<TKey, TValue> OnAdd() => new DictTrapBuilder<TKey, TValue>(this, TrapEventType.Added);
        public DictTrapBuilder<TKey, TValue> OnRemove() => new DictTrapBuilder<TKey, TValue>(this, TrapEventType.Removed);
        public DictTrapBuilder<TKey, TValue> OnUpdate() => new DictTrapBuilder<TKey, TValue>(this, TrapEventType.Updated);
        public DictTrapBuilder<TKey, TValue> OnClear() => new DictTrapBuilder<TKey, TValue>(this, TrapEventType.Cleared);

        public void AddRule(DictTrapRule<TKey, TValue> rule)
        {
            lock (_trapLock)
            {
                var existing = _ruleBuckets[rule.EventType];
                var newArray = new DictTrapRule<TKey, TValue>[existing.Length + 1];
                Array.Copy(existing, newArray, existing.Length);
                newArray[existing.Length] = rule;
                _ruleBuckets[rule.EventType] = newArray;
            }
        }

        // --- Direct Access Methods (Bypass Traps) ---

        public bool TryAddWithoutTrap(TKey key, TValue value)
        {
            return _inner.TryAdd(key, value);
        }

        // --- Core Interception Logic ---

        public TValue this[TKey key]
        {
            get => _inner[key];
            set
            {
                // In a concurrent environment, this is tricky. 
                // We use AddOrUpdate to ensure atomicity, and trigger traps inside the delegates.
                _inner.AddOrUpdate(
                    key,
                    addValueFactory: (k) => 
                    {
                        ExecuteTraps(TrapEventType.Added, k, value);
                        return value;
                    },
                    updateValueFactory: (k, existingVal) => 
                    {
                        ExecuteTraps(TrapEventType.Updated, k, value, existingVal);
                        return value;
                    });
            }
        }

        public void Add(TKey key, TValue value)
        {
            if (TryAdd(key, value) == false)
            {
                throw new ArgumentException("An item with the same key has already been added.");
            }
        }

        public bool TryAdd(TKey key, TValue value)
        {
            // First we try to add. If successful, we trigger the trap.
            // Note: In highly concurrent scenarios, triggering the trap *after* the add 
            // is safer than before, because another thread might beat us to the add.
            if (_inner.TryAdd(key, value))
            {
                ExecuteTraps(TrapEventType.Added, key, value);
                return true;
            }
            return false;
        }

        public bool Remove(TKey key)
        {
            return TryRemove(key, out _);
        }

        public bool TryRemove(TKey key, out TValue value)
        {
            if (_inner.TryRemove(key, out value))
            {
                ExecuteTraps(TrapEventType.Removed, key, value);
                return true;
            }
            return false;
        }

        public bool TryUpdate(TKey key, TValue newValue, TValue comparisonValue)
        {
            if (_inner.TryUpdate(key, newValue, comparisonValue))
            {
                ExecuteTraps(TrapEventType.Updated, key, newValue, comparisonValue);
                return true;
            }
            return false;
        }

        public void Clear()
        {
            ExecuteTraps(TrapEventType.Cleared, default!, default!);
            _inner.Clear();
        }

        // --- IDictionary / ICollection implementations ---

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        {
            if (_inner.TryGetValue(item.Key, out var val) && EqualityComparer<TValue>.Default.Equals(val, item.Value))
            {
                return TryRemove(item.Key, out _);
            }
            return false;
        }

        public ICollection<TKey> Keys => _inner.Keys;
        public ICollection<TValue> Values => _inner.Values;
        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => _inner.Keys;
        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => _inner.Values;
        public int Count => _inner.Count;
        public bool IsReadOnly => false;
        public bool ContainsKey(TKey key) => _inner.ContainsKey(key);
        public bool TryGetValue(TKey key, out TValue value) 
        {
#pragma warning disable CS8601 // Possible null reference assignment.
            return _inner.TryGetValue(key, out value);
#pragma warning restore CS8601
        }
        public bool Contains(KeyValuePair<TKey, TValue> item) => ((ICollection<KeyValuePair<TKey, TValue>>)_inner).Contains(item);
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => ((ICollection<KeyValuePair<TKey, TValue>>)_inner).CopyTo(array, arrayIndex);
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _inner.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _inner.GetEnumerator();


        // --- Execution Engine ---

        private void ExecuteTraps(TrapEventType eventType, TKey key, TValue value, TValue? oldValue = default!)
        {
            if (!TrapManager.Enabled) return;

            var rules = _ruleBuckets[eventType];
            if (rules.Length == 0) return;

            for (int i = 0; i < rules.Length; i++)
            {
                var rule = rules[i];
                try
                {
                    if (rule.Predicate == null || rule.Predicate(key, value))
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
}
