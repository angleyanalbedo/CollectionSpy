<div align="center">

# 🕵️‍♂️ CollectionSpy 

**The Zero-Friction Debugging Toolkit for C# Collections**

[![NuGet](https://img.shields.io/nuget/v/CollectionSpy.svg?style=flat-square&color=blue)](https://www.nuget.org/packages/CollectionSpy/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/CollectionSpy.svg?style=flat-square&color=blue)](https://www.nuget.org/packages/CollectionSpy/)
[![License](https://img.shields.io/github/license/angleyanalbedo/CollectionSpy?style=flat-square&color=green)](LICENSE)
[![.NET 8.0](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/)

*Stop guessing **who** modified your data. Trap them red-handed.*

[English](README.md) | [简体中文](docs/README.zh-CN.md)

</div>

---

## 🚀 Why CollectionSpy?

Debugging complex state management, legacy code, or high-frequency data streams (like PLC signals in industrial automation) can be a nightmare when a `List` or `Dictionary` is modified unexpectedly.

- ❌ **`ObservableCollection`** is too verbose for temporary debugging and pollutes your architecture.
- ❌ **Conditional Breakpoints** in IDEs (like Visual Studio) are painfully slow and cannot be shared with your team.
- ❌ **AOP Frameworks** (like PostSharp) are heavy, slow to compile, and overkill for simple debugging tasks.

**CollectionSpy** solves this with a fluent, declarative API. It provides drop-in replacements (`TrapList`, `TrapDictionary`, etc.) that let you inject **Breakpoints**, **Stack Traces**, or **Custom Logs** exactly when specific data conditions are met.

---

## 📦 Installation

Grab the latest version from NuGet:

```bash
dotnet add package CollectionSpy
```

Or via the Package Manager Console:
```powershell
Install-Package CollectionSpy
```

---

## ⚡ Quick Start

### 1. Spy on a List

You can create a `TrapList` directly, or convert any existing `IEnumerable` using the elegant fluent extensions:

```csharp
using Debugging.Traps;
using Debugging.Traps.Extensions; // Gives you .ToTrapList(), .ToTrapDictionary(), etc.

// Instead of new List<User>(), just do this:
var users = GetUsers().ToTrapList(); 

// 🎯 Scenario 1: Break execution when a bad object is added
users.OnAdd()
     .When(u => u.Name == null) // The condition
     .Do(TrapActions.Break());  // The trap (Debugger.Break)

// 🎯 Scenario 2: Log a warning when a specific critical ID is removed
users.OnRemove()
     .When(u => u.Id == 999)
     .Do(TrapActions.Log("🚨 WARNING: Admin user 999 was removed!"));
```

### 2. Spy on a Dictionary

Perfect for monitoring configuration changes or caching layers.

```csharp
var config = GetConfigs().ToTrapDictionary();

// 🚨 Alert if a secure setting is downgraded to HTTP
config.OnUpdate()
      .When((key, value) => key == "ApiUrl" && value.StartsWith("http:"))
      .Do(TrapActions.Log("SECURITY ALERT: API URL downgraded to insecure HTTP!"));
```

### 3. Spy on a HashSet

Catch inefficient code logic or duplicate entries easily.

```csharp
var uniqueTags = new TrapHashSet<string>();

// 🐌 Detect inefficient code: attempting to add a tag that is already present
uniqueTags.OnAdd()
          .When(tag => uniqueTags.Contains(tag))
          .Do(TrapActions.Log("Inefficient Code: Tag already exists in the set!"));
```

### 4. Spy on Queues and Stacks

```csharp
// Spy on a Queue (FIFO) - Great for Task processing
var jobQueue = new List<string> { "init_job" }.ToTrapQueue();

jobQueue.OnEnqueue()
        .When(job => job == "POISON_PILL")
        .Do(TrapActions.Log("Critical: Poison pill enqueued!"));

// Spy on a Stack (LIFO) - Great for UI Navigation tracking
var navStack = new TrapStack<string>();

navStack.OnPop()
        .When(page => page == "Root")
        .Do(TrapActions.DumpStackTrace("Root Page Popped By:"));
```

---

## 🛡️ Performance & Production Safety

**CollectionSpy** is engineered for critical debugging in production environments with virtually zero overhead when you need it to be fast.

### ⚡ Zero-Allocation Architecture (v1.0+)
The library uses a highly optimized **Copy-On-Write** strategy for rule storage.
- **Zero Allocations**: Executing traps (adding/removing items) incurs **0 bytes of memory allocation** on the hot path. No closures or hidden objects are created during enumeration.
- **Microsecond Overhead**: Even with active traps evaluating conditions, the overhead is measured in single-digit microseconds.
- **Thread Safe Configuration**: Rule addition and removal are lock-free and thread-safe.

### 📊 Benchmark Snippet
Comparison of `List<int>.Add()` operations (10,000 items):

| Method | Mean Time | Ratio | Gen0 Allocations |
| :--- | :--- | :--- | :--- |
| **Native `List<T>`** | ~7.9 μs | 1.0x | - |
| **`AddWithoutTrap`** | ~9.7 μs | 1.2x | - |
| **`TrapList` (Active)** | ~71.0 μs | 8.9x | - |

> *Benchmarks run on AMD Ryzen 9 8945HX, .NET 8.0. Check [BENCHMARKS.md](BENCHMARKS.md) for full details.*

### 🚀 Bypassing Traps (Bulk Inserts)
Need to initialize a large collection without triggering traps and ruining performance? Use the **Bypass API**:

```csharp
// Fast bulk insert, ZERO trap overhead
myTrapList.AddWithoutTrap(newItem);
myTrapList.AddRange(largeCollection); // Native speed
```

### 🎛️ The Global Kill Switch
To disable all overhead in production, simply flip the master switch:

```csharp
TrapManager.Enabled = false;
```
When disabled, the interception logic returns immediately (fast-fail), imposing negligible overhead. Note that in **Release** builds, the library will emit a single **Console Warning** on startup to alert you if traps are left active.

---

## 🗺️ Roadmap & Next Steps

We are actively evolving CollectionSpy from a "handy tool" to an "industrial-grade framework". Check out our [Roadmap](docs/ROADMAP.md) for upcoming features, including:
- 🚀 `INotifyCollectionChanged` support for WPF/WinForms data binding.
- 🏭 Thread-safe `ConcurrentDictionary` and `ConcurrentQueue` support.
- ⚡ **Source Generators** for true zero-overhead, AOT-friendly compilation.

## 🤝 Contributing

Contributions, issues, and feature requests are welcome! 
Feel free to check [issues page](https://github.com/angleyanalbedo/CollectionSpy/issues).

## 📝 License

This project is [MIT](LICENSE) licensed.

---
*Crafted with ❤️ by [angleyanalbedo](https://github.com/angleyanalbedo)*