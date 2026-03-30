namespace Debugging.Traps
{
    internal interface ITrapDictionaryTarget<TKey, TValue> where TKey : notnull
    {
        void AddRule(DictTrapRule<TKey, TValue> rule);
    }
}
