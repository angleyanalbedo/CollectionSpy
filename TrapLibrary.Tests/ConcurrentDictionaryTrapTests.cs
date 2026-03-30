using Xunit;
using Debugging.Traps;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Linq;

namespace Debugging.Traps.Tests
{
    [Collection("Sequential")]
    public class ConcurrentDictionaryTrapTests
    {
        public ConcurrentDictionaryTrapTests()
        {
            TrapManager.Enabled = true;
        }

        [Fact]
        public void Add_Should_Trigger_Trap_And_Be_ThreadSafe()
        {
            var dict = new TrapConcurrentDictionary<int, string>();
            int trapCount = 0;

            dict.OnAdd()
                .WhenValue(v => v.StartsWith("Item"))
                .Do(() => System.Threading.Interlocked.Increment(ref trapCount));

            Parallel.For(0, 1000, i => 
            {
                dict.TryAdd(i, $"Item{i}");
            });

            Assert.Equal(1000, dict.Count);
            Assert.Equal(1000, trapCount);
        }

        [Fact]
        public void Update_Should_Trigger_Trap_Safely()
        {
            var dict = new TrapConcurrentDictionary<int, string>();
            dict.TryAdd(1, "Old");
            
            bool trapTriggered = false;

            dict.OnUpdate()
                .When((k, v) => k == 1 && v == "New")
                .Do(() => trapTriggered = true);

            dict[1] = "New";

            Assert.True(trapTriggered);
            Assert.Equal("New", dict[1]);
        }
        
        [Fact]
        public void Remove_Should_Trigger_Trap()
        {
            var dict = new TrapConcurrentDictionary<int, string>();
            dict.TryAdd(99, "Secret");
            
            bool trapTriggered = false;

            dict.OnRemove()
                .WhenKey(k => k == 99)
                .Do(() => trapTriggered = true);

            dict.TryRemove(99, out _);

            Assert.True(trapTriggered);
            Assert.Empty(dict);
        }
    }
}
