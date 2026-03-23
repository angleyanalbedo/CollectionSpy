using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Debugging.Traps
{
    public class TrapStack<T> : IEnumerable<T>, IReadOnlyCollection<T>
    {
        private readonly Stack<T> _inner;
        private readonly object _trapLock = new object();
        private readonly Dictionary<TrapEventType, StackTrapRule<T>[]> _ruleBuckets;

        public TrapStack() 
        { 
            _inner = new Stack<T>(); 
            _ruleBuckets = InitializeBuckets();
        }
        public TrapStack(IEnumerable<T> collection) 
        { 
            _inner = new Stack<T>(collection); 
            _ruleBuckets = InitializeBuckets();
        }
        public TrapStack(int capacity) 
        { 
            _inner = new Stack<T>(capacity); 
            _ruleBuckets = InitializeBuckets();
        }

        private Dictionary<TrapEventType, StackTrapRule<T>[]> InitializeBuckets()
        {
            var dict = new Dictionary<TrapEventType, StackTrapRule<T>[]>();
            foreach (TrapEventType type in Enum.GetValues(typeof(TrapEventType)))
            {
                dict[type] = new StackTrapRule<T>[0];
            }
            return dict;
        }

        // --- Fluent API ---
        public StackTrapBuilder<T> OnPush() => new StackTrapBuilder<T>(this, TrapEventType.Added);
        public StackTrapBuilder<T> OnPop() => new StackTrapBuilder<T>(this, TrapEventType.Removed);
        public StackTrapBuilder<T> OnClear() => new StackTrapBuilder<T>(this, TrapEventType.Cleared);
        
        internal void AddRule(StackTrapRule<T> rule) 
        { 
            lock(_trapLock) 
            {
                var existing = _ruleBuckets[rule.EventType];
                var newArray = new StackTrapRule<T>[existing.Length + 1];
                Array.Copy(existing, newArray, existing.Length);
                newArray[existing.Length] = rule;
                _ruleBuckets[rule.EventType] = newArray;
            }
        }

        // --- Core Methods ---
        public void Push(T item)
        {
            ExecuteTraps(TrapEventType.Added, item);
            _inner.Push(item);
        }

        public T Pop()
        {
             if (_inner.Count == 0) throw new InvalidOperationException("Stack empty");
             var item = _inner.Peek();
             ExecuteTraps(TrapEventType.Removed, item);
             return _inner.Pop();
        }

        public bool TryPop(out T result)
        {
            if (_inner.Count == 0)
            {
                result = default!;
                return false;
            }
            result = Pop();
            return true;
        }

        public T Peek() => _inner.Peek();

        public bool TryPeek(out T result)
        {
            if (_inner.Count == 0)
            {
                result = default!;
                return false;
            }
            result = _inner.Peek();
            return true;
        }

        public void Clear()
        {
            ExecuteTraps(TrapEventType.Cleared, default!);
            _inner.Clear();
        }
        
        public bool Contains(T item) => _inner.Contains(item);
        public T[] ToArray() => _inner.ToArray();
        public void TrimExcess() => _inner.TrimExcess();
        
        public int Count => _inner.Count;
        public IEnumerator<T> GetEnumerator() => _inner.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _inner.GetEnumerator();

        // --- Execution ---
        private void ExecuteTraps(TrapEventType eventType, T item)
        {
            if (!TrapManager.Enabled) return;

            var rules = _ruleBuckets[eventType];
            if (rules.Length == 0) return;

            for (int i = 0; i < rules.Length; i++)
            {
                var rule = rules[i];
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

    public class StackTrapRule<T>
    {
        public TrapEventType EventType { get; set; }
        public Func<T, bool>? Predicate { get; set; }
        public Action? Action { get; set; }
    }

    public class StackTrapBuilder<T>
    {
        private readonly TrapStack<T> _stack;
        private readonly TrapEventType _type;
        private Func<T, bool>? _predicate;

        public StackTrapBuilder(TrapStack<T> stack, TrapEventType type)
        {
            _stack = stack;
            _type = type;
        }

        public StackTrapBuilder<T> When(Func<T, bool> predicate)
        {
            _predicate = predicate;
            return this;
        }

        public void Do(Action action)
        {
            _stack.AddRule(new StackTrapRule<T> { EventType = _type, Predicate = _predicate, Action = action });
        }
    }
}
