using Xunit;
using Debugging.Traps;

namespace Debugging.Traps.Tests
{
    [Collection("Sequential")]
    public class TrapManagerTests
    {
        [Fact]
        public void Disabled_Manager_Should_Prevent_Action()
        {
            // Arrange
            bool wasEnabled = TrapManager.Enabled;
            TrapManager.Enabled = false; // DISABLE
            
            var list = new TrapList<int>();
            bool fired = false;
            list.OnAdd().Do(() => fired = true);

            // Act
            list.Add(1);

            // Assert
            Assert.False(fired, "Action should NOT fire when TrapManager is disabled");

            // Cleanup
            TrapManager.Enabled = wasEnabled;
        }

        [Fact]
        public void Enabled_Manager_Should_Allow_Action()
        {
            // Arrange
            TrapManager.Enabled = true; // ENABLE
            
            var list = new TrapList<int>();
            bool fired = false;
            list.OnAdd().Do(() => fired = true);

            // Act
            list.Add(1);

            // Assert
            Assert.True(fired);
        }
    }
}
