using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Debugging.Traps;
using System.Collections.Generic;

namespace TrapLibrary.Benchmarks
{
    [MemoryDiagnoser]
    [ShortRunJob]
    public class ListBenchmark
    {
        private List<int> _nativeList;
        private TrapList<int> _trapListNoRules;
        private TrapList<int> _trapListWithRule;
        private TrapList<int> _trapListBypass;

        [Params(1000, 10000)]
        public int N;

        [GlobalSetup]
        public void Setup()
        {
            _nativeList = new List<int>();
            _trapListNoRules = new TrapList<int>();
            
            _trapListWithRule = new TrapList<int>();
            _trapListWithRule.OnAdd().Do(() => { /* Empty Action */ });

            _trapListBypass = new TrapList<int>();
        }

        [Benchmark(Baseline = true)]
        public void NativeList_Add()
        {
            _nativeList.Clear();
            for (int i = 0; i < N; i++)
            {
                _nativeList.Add(i);
            }
        }

        [Benchmark]
        public void TrapList_Add_NoRules()
        {
            _trapListNoRules.Clear();
            for (int i = 0; i < N; i++)
            {
                _trapListNoRules.Add(i);
            }
        }

        [Benchmark]
        public void TrapList_Add_WithRule()
        {
            _trapListWithRule.Clear();
            for (int i = 0; i < N; i++)
            {
                _trapListWithRule.Add(i);
            }
        }

        [Benchmark]
        public void TrapList_AddWithoutTrap()
        {
            _trapListBypass.Clear();
            for (int i = 0; i < N; i++)
            {
                _trapListBypass.AddWithoutTrap(i);
            }
        }
    }
}