using Xunit;
using Debugging.Traps;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Collections.Generic;

namespace Debugging.Traps.Tests
{
    [Collection("Sequential")]
    public class NotifyCollectionChangedTests
    {
        [Fact]
        public void TrapList_Add_ShouldTriggerEvents()
        {
            var list = new TrapList<string>();
            var collectionChanges = new List<NotifyCollectionChangedEventArgs>();
            var propertyChanges = new List<string>();

            list.CollectionChanged += (s, e) => collectionChanges.Add(e);
            list.PropertyChanged += (s, e) => propertyChanges.Add(e.PropertyName);

            list.Add("TestItem");

            Assert.Single(collectionChanges);
            Assert.Equal(NotifyCollectionChangedAction.Add, collectionChanges[0].Action);
            Assert.Equal("TestItem", collectionChanges[0].NewItems[0]);

            Assert.Contains("Count", propertyChanges);
            Assert.Contains("Item[]", propertyChanges);
        }

        [Fact]
        public void TrapList_Remove_ShouldTriggerEvents()
        {
            var list = new TrapList<string> { "TestItem" };
            var collectionChanges = new List<NotifyCollectionChangedEventArgs>();
            var propertyChanges = new List<string>();

            list.CollectionChanged += (s, e) => collectionChanges.Add(e);
            list.PropertyChanged += (s, e) => propertyChanges.Add(e.PropertyName);

            list.Remove("TestItem");

            Assert.Single(collectionChanges);
            Assert.Equal(NotifyCollectionChangedAction.Remove, collectionChanges[0].Action);
            Assert.Equal("TestItem", collectionChanges[0].OldItems[0]);

            Assert.Contains("Count", propertyChanges);
            Assert.Contains("Item[]", propertyChanges);
        }
        
        [Fact]
        public void TrapList_Set_ShouldTriggerEvents()
        {
            var list = new TrapList<string> { "OldItem" };
            var collectionChanges = new List<NotifyCollectionChangedEventArgs>();
            var propertyChanges = new List<string>();

            list.CollectionChanged += (s, e) => collectionChanges.Add(e);
            list.PropertyChanged += (s, e) => propertyChanges.Add(e.PropertyName);

            list[0] = "NewItem";

            Assert.Single(collectionChanges);
            Assert.Equal(NotifyCollectionChangedAction.Replace, collectionChanges[0].Action);
            Assert.Equal("NewItem", collectionChanges[0].NewItems[0]);
            Assert.Equal("OldItem", collectionChanges[0].OldItems[0]);

            Assert.DoesNotContain("Count", propertyChanges);
            Assert.Contains("Item[]", propertyChanges);
        }

        [Fact]
        public void TrapList_Clear_ShouldTriggerEvents()
        {
            var list = new TrapList<string> { "Item1", "Item2" };
            var collectionChanges = new List<NotifyCollectionChangedEventArgs>();
            var propertyChanges = new List<string>();

            list.CollectionChanged += (s, e) => collectionChanges.Add(e);
            list.PropertyChanged += (s, e) => propertyChanges.Add(e.PropertyName);

            list.Clear();

            Assert.Single(collectionChanges);
            Assert.Equal(NotifyCollectionChangedAction.Reset, collectionChanges[0].Action);

            Assert.Contains("Count", propertyChanges);
            Assert.Contains("Item[]", propertyChanges);
        }
    }
}
