using Xunit;
using Debugging.Traps;

namespace Debugging.Traps.Tests
{
    [Collection("Sequential")]
    public class TrapQueueStackTests
    {
        public TrapQueueStackTests()
        {
            TrapManager.Enabled = true;
        }

        [Fact]
        public void Queue_Enqueue_Dequeue_Should_Trap()
        {
            var queue = new TrapQueue<int>();
            bool added = false;
            int removedItem = -1;

            queue.OnEnqueue().Do(() => added = true);
            // Warning: accessing queue inside Do() while dequeuing is tricky, 
            // but here Dequeue logic triggers trap *before* removing.
            // So Peek() should still return the item being removed.
            queue.OnDequeue().Do(() => removedItem = queue.Peek()); 

            queue.Enqueue(10);
            Assert.True(added);

            var item = queue.Dequeue();
            Assert.Equal(10, item);
            Assert.Equal(10, removedItem);
        }

        [Fact]
        public void Stack_Push_Pop_Should_Trap()
        {
            var stack = new TrapStack<string>();
            string popped = null;

            stack.OnPop().When(x => x == "B").Do(() => popped = "B");

            stack.Push("A");
            stack.Push("B");
            
            stack.Pop(); // B
            Assert.Equal("B", popped);

            popped = null;
            stack.Pop(); // A
            Assert.Null(popped); // Should not trap A
        }
    }
}
