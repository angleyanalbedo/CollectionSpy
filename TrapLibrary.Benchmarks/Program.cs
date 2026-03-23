using BenchmarkDotNet.Running;

namespace TrapLibrary.Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<ListBenchmark>();
        }
    }
}