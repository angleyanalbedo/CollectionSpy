using Xunit;
using Debugging.Traps;
using System.Collections.Generic;

namespace Debugging.Traps.Tests
{
    [Collection("Sequential")]
    public class DictionaryTrapTests
    {
        public DictionaryTrapTests()
        {
            TrapManager.Enabled = true;
        }

        [Fact]
        public void Add_Should_Trigger()
        {
            var dict = new TrapDictionary<string, int>();
            bool added = false;
            
            dict.OnAdd().Do(() => added = true);
            
            dict.Add("A", 1);
            
            Assert.True(added);
            Assert.Equal(1, dict["A"]);
        }

        [Fact]
        public void Indexer_Set_Should_Trigger_Add_Or_Update()
        {
            var dict = new TrapDictionary<string, int>();
            int addCount = 0;
            int updateCount = 0;

            dict.OnAdd().Do(() => addCount++);
            dict.OnUpdate().Do(() => updateCount++);

            dict["A"] = 1; // Add
            Assert.Equal(1, addCount);
            Assert.Equal(0, updateCount);

            dict["A"] = 2; // Update
            Assert.Equal(1, addCount);
            Assert.Equal(1, updateCount);
        }

        [Fact]
        public void Remove_Via_Interface_Should_Trigger()
        {
            var dict = new TrapDictionary<string, int> { { "A", 1 } };
            bool removed = false;

            dict.OnRemove().WhenKey(k => k == "A").Do(() => removed = true);

            IDictionary<string, int> iDict = dict;
            iDict.Remove("A");

            Assert.True(removed);
            Assert.Empty(dict);
        }
    }
}
