using Xunit;
using Debugging.Traps;
using System.Collections.Generic;

namespace Debugging.Traps.Tests
{
    [Collection("Sequential")]
    public class ListTrapTests
    {
        public ListTrapTests()
        {
            // Ensure traps are enabled for tests
            TrapManager.Enabled = true;
        }

        [Fact]
        public void Add_Via_IList_Should_Trigger_Trap()
        {
            // Arrange
            var list = new TrapList<int>();
            bool trapped = false;
            
            list.OnAdd()
                .When(x => x > 10)
                .Do(() => trapped = true);

            // Act - IMPORTANT: Cast to interface to verify polymorphism
            IList<int> iList = list;
            iList.Add(5); // Should not trigger
            Assert.False(trapped, "Should not trap 5");

            iList.Add(15); // Should trigger

            // Assert
            Assert.True(trapped, "Should trap 15 even via IList interface");
            Assert.Contains(15, list);
        }

        [Fact]
        public void SetItem_Should_Trigger_Update_Trap()
        {
            var list = new TrapList<string> { "A", "B" };
            bool fired = false;
            
            list.OnUpdate().Do(() => fired = true);

            list[1] = "C";

            Assert.True(fired);
            Assert.Equal("C", list[1]);
        }

        [Fact]
        public void Clear_Should_Trigger()
        {
            var list = new TrapList<int> { 1, 2, 3 };
            bool cleared = false;
            
            list.OnClear().Do(() => cleared = true);
            
            list.Clear();
            
            Assert.True(cleared);
            Assert.Empty(list);
        }

        [Fact]
        public void Remove_Predicate_Should_Work()
        {
            var list = new TrapList<int> { 1, 2, 3 };
            int removedItem = 0;

            list.OnRemove()
                .When(x => x == 2)
                .Do(() => removedItem = 2);

            list.Remove(1); // Should not trigger action
            Assert.Equal(0, removedItem);

            list.Remove(2); // Should trigger
            Assert.Equal(2, removedItem);
        }
    }
}
