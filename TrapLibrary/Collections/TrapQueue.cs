using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Debugging.Traps
{
    public class TrapQueue<T> : IEnumerable<T>, IReadOnlyCollection<T>
    {
        private readonly Queue<T> _inner;
        private readonly object _trapLock = new object();
        private readonly Dictionary<TrapEventType, QueueTrapRule<T>[]> _ruleBuckets;

        public TrapQueue() 
        { 
            _inner = new Queue<T>();
            _ruleBuckets = InitializeBuckets();
        }
        public TrapQueue(IEnumerable<T> collection) 
        { 
            _inner = new Queue<T>(collection);
            _ruleBuckets = InitializeBuckets();
        }
        public TrapQueue(int capacity) 
        { 
            _inner = new Queue<T>(capacity);
            _ruleBuckets = InitializeBuckets();
        }

        private Dictionary<TrapEventType, QueueTrapRule<T>[]> InitializeBuckets()
        {
            var dict = new Dictionary<TrapEventType, QueueTrapRule<T>[]>();
            foreach (TrapEventType type in Enum.GetValues(typeof(TrapEventType)))
            {
                dict[type] = new QueueTrapRule<T>[0];
            }
            return dict;
        }

        // --- Fluent API ---
        public QueueTrapBuilder<T> OnEnqueue() => new QueueTrapBuilder<T>(this, TrapEventType.Added);
        public QueueTrapBuilder<T> OnDequeue() => new QueueTrapBuilder<T>(this, TrapEventType.Removed);
        public QueueTrapBuilder<T> OnClear() => new QueueTrapBuilder<T>(this, TrapEventType.Cleared);
        
        internal void AddRule(QueueTrapRule<T> rule) 
        { 
            lock(_trapLock)
            {
                var existing = _ruleBuckets[rule.EventType];
                var newArray = new QueueTrapRule<T>[existing.Length + 1];
                Array.Copy(existing, newArray, existing.Length);
                newArray[existing.Length] = rule;
                _ruleBuckets[rule.EventType] = newArray;
            }
        }

        // --- Core Methods ---
        public void Enqueue(T item)
        {
            ExecuteTraps(TrapEventType.Added, item);
            _inner.Enqueue(item);
        }

        public T Dequeue()
        {
             if (_inner.Count == 0) throw new InvalidOperationException("Queue empty");
             var item = _inner.Peek();
             ExecuteTraps(TrapEventType.Removed, item);
             return _inner.Dequeue();
        }

        public bool TryDequeue(out T result)
        {
            if (_inner.Count == 0)
            {
                result = default!;
                return false;
            }
            result = Dequeue(); // Will trigger traps
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

    public class QueueTrapRule<T>
    {
        public TrapEventType EventType { get; set; }
        public Func<T, bool>? Predicate { get; set; }
        public Action? Action { get; set; }
    }

    public class QueueTrapBuilder<T>
    {
        private readonly TrapQueue<T> _queue;
        private readonly TrapEventType _type;
        private Func<T, bool>? _predicate;

        public QueueTrapBuilder(TrapQueue<T> queue, TrapEventType type)
        {
            _queue = queue;
            _type = type;
        }

        public QueueTrapBuilder<T> When(Func<T, bool> predicate)
        {
            _predicate = predicate;
            return this;
        }

        public void Do(Action action)
        {
            _queue.AddRule(new QueueTrapRule<T> { EventType = _type, Predicate = _predicate, Action = action });
        }
    }
}
