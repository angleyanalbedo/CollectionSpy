```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8037/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 8945HX with Radeon Graphics 2.50GHz, 1 CPU, 32 logical and 16 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 8.0.25 (8.0.25, 8.0.2526.11203), X64 RyuJIT x86-64-v4
  ShortRun : .NET 8.0.25 (8.0.25, 8.0.2526.11203), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                  | N     | Mean        | Error        | StdDev      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------ |------ |------------:|-------------:|------------:|------:|--------:|----------:|------------:|
| **NativeList_Add**          | **1000**  |    **769.5 ns** |     **428.7 ns** |    **23.50 ns** |  **1.00** |    **0.04** |         **-** |          **NA** |
| TrapList_Add_NoRules    | 1000  |  5,429.5 ns |     906.3 ns |    49.68 ns |  7.06 |    0.20 |         - |          NA |
| TrapList_Add_WithRule   | 1000  |  7,200.0 ns |   3,100.8 ns |   169.97 ns |  9.36 |    0.32 |         - |          NA |
| TrapList_AddWithoutTrap | 1000  |  1,025.8 ns |     496.3 ns |    27.20 ns |  1.33 |    0.05 |         - |          NA |
|                         |       |             |              |             |       |         |           |             |
| **NativeList_Add**          | **10000** |  **7,932.9 ns** |   **2,897.7 ns** |   **158.83 ns** |  **1.00** |    **0.02** |         **-** |          **NA** |
| TrapList_Add_NoRules    | 10000 | 55,640.2 ns | 100,136.4 ns | 5,488.81 ns |  7.02 |    0.61 |         - |          NA |
| TrapList_Add_WithRule   | 10000 | 71,079.2 ns |  40,892.6 ns | 2,241.46 ns |  8.96 |    0.29 |         - |          NA |
| TrapList_AddWithoutTrap | 10000 |  9,778.1 ns |   9,962.4 ns |   546.07 ns |  1.23 |    0.06 |         - |          NA |
