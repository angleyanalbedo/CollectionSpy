using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Debugging.Traps
{
    public class TrapHashSet<T> : ISet<T>
    {
        private readonly HashSet<T> _inner;
        private readonly object _trapLock = new object();
        private readonly List<SetTrapRule<T>> _rules = new List<SetTrapRule<T>>();

        public TrapHashSet()
        {
            _inner = new HashSet<T>();
        }

        public TrapHashSet(IEnumerable<T> collection)
        {
            _inner = new HashSet<T>(collection);
        }

        public TrapHashSet(IEqualityComparer<T> comparer)
        {
            _inner = new HashSet<T>(comparer);
        }

        // --- Fluent API Entry Points ---
        public SetTrapBuilder<T> OnAdd() => new SetTrapBuilder<T>(this, TrapEventType.Added);
        public SetTrapBuilder<T> OnRemove() => new SetTrapBuilder<T>(this, TrapEventType.Removed);
        public SetTrapBuilder<T> OnClear() => new SetTrapBuilder<T>(this, TrapEventType.Cleared);

        internal void AddRule(SetTrapRule<T> rule)
        {
            lock (_trapLock)
            {
                _rules.Add(rule);
            }
        }

        // --- Direct Access Methods (Bypass Traps) ---

        public bool AddWithoutTrap(T item)
        {
            return _inner.Add(item);
        }

        public void AddRange(IEnumerable<T> collection)
        {
            foreach (var item in collection)
            {
                _inner.Add(item);
            }
        }

        // --- Core Interception Logic (ISet Implementation) ---

        void ICollection<T>.Add(T item)
        {
            Add(item);
        }

        bool ISet<T>.Add(T item)
        {
            return Add(item);
        }

        public bool Add(T item)
        {
            // Execute trap before adding to catch the attempt
            ExecuteTraps(TrapEventType.Added, item);
            return _inner.Add(item);
        }

        public void UnionWith(IEnumerable<T> other)
        {
            foreach (var item in other) Add(item);
        }

        public void IntersectWith(IEnumerable<T> other)
        {
            // Intersection implies removal of elements not in 'other'
            // To ensure Traps fire for removal, we must iterate.
            // Note: This is less performant than native IntersectWith but necessary for debugging.
            if (_inner.Count == 0) return;

            var otherSet = new HashSet<T>(other, _inner.Comparer);
            var toRemove = _inner.Where(item => !otherSet.Contains(item)).ToList();
            foreach (var item in toRemove)
            {
                Remove(item);
            }
        }

        public void ExceptWith(IEnumerable<T> other)
        {
            foreach (var item in other)
            {
                Remove(item);
            }
        }

        public void SymmetricExceptWith(IEnumerable<T> other)
        {
            foreach (var item in other)
            {
                if (_inner.Contains(item))
                {
                    Remove(item);
                }
                else
                {
                    Add(item);
                }
            }
        }

        public bool IsSubsetOf(IEnumerable<T> other) => _inner.IsSubsetOf(other);
        public bool IsSupersetOf(IEnumerable<T> other) => _inner.IsSupersetOf(other);
        public bool IsProperSupersetOf(IEnumerable<T> other) => _inner.IsProperSupersetOf(other);
        public bool IsProperSubsetOf(IEnumerable<T> other) => _inner.IsProperSubsetOf(other);
        public bool Overlaps(IEnumerable<T> other) => _inner.Overlaps(other);
        public bool SetEquals(IEnumerable<T> other) => _inner.SetEquals(other);

        public void Clear()
        {
            ExecuteTraps(TrapEventType.Cleared, default!);
            _inner.Clear();
        }

        public bool Contains(T item) => _inner.Contains(item);

        public void CopyTo(T[] array, int arrayIndex) => _inner.CopyTo(array, arrayIndex);

        public bool Remove(T item)
        {
            if (_inner.Contains(item))
            {
                ExecuteTraps(TrapEventType.Removed, item);
                return _inner.Remove(item);
            }
            return false;
        }

        public int Count => _inner.Count;
        public bool IsReadOnly => false;

        public IEnumerator<T> GetEnumerator() => _inner.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _inner.GetEnumerator();

        // --- Execution Engine ---

        private void ExecuteTraps(TrapEventType eventType, T item)
        {
            if (!TrapManager.Enabled || _rules.Count == 0) return;

            List<SetTrapRule<T>> activeRules;
            lock (_trapLock)
            {
                activeRules = _rules.Where(r => r.EventType == eventType).ToList();
            }

            foreach (var rule in activeRules)
            {
                try
                {
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

    // --- HashSet Related Builder and Rule ---

    public class SetTrapRule<T>
    {
        public TrapEventType EventType { get; set; }
        public Func<T, bool>? Predicate { get; set; }
        public Action? Action { get; set; }
    }

    public class SetTrapBuilder<T>
    {
        private readonly TrapHashSet<T> _set;
        private readonly TrapEventType _type;
        private Func<T, bool>? _predicate;

        public SetTrapBuilder(TrapHashSet<T> set, TrapEventType type)
        {
            _set = set;
            _type = type;
        }

        public SetTrapBuilder<T> When(Func<T, bool> predicate)
        {
            _predicate = predicate;
            return this;
        }

        public void Do(Action action)
        {
            var rule = new SetTrapRule<T>
            {
                EventType = _type,
                Predicate = _predicate,
                Action = action
            };
            _set.AddRule(rule);
        }
    }
}
