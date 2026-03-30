<div align="center">

# 🕵️‍♂️ CollectionSpy 

**C# 集合零摩擦调试工具包**

[![NuGet](https://img.shields.io/nuget/v/CollectionSpy.svg?style=flat-square&color=blue)](https://www.nuget.org/packages/CollectionSpy/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/CollectionSpy.svg?style=flat-square&color=blue)](https://www.nuget.org/packages/CollectionSpy/)
[![License](https://img.shields.io/github/license/angleyanalbedo/CollectionSpy?style=flat-square&color=green)](LICENSE)
[![.NET 8.0](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/)

*不要再猜测是**谁**修改了你的数据，直接当场“抓获”他们。*

[English](../README.md) | [简体中文](README.zh-CN.md)

</div>

---

## 🚀 为什么选择 CollectionSpy？

在工业自动化（例如 PLC 信号）或复杂的业务状态管理中，当 `List` 或 `Dictionary` 里的数据被意外修改时，排查问题往往是一场噩梦。

- ❌ **`ObservableCollection`** 对于临时调试来说过于繁琐，且会污染现有的架构。
- ❌ **条件断点 (Conditional Breakpoints)** 在 Visual Studio 中执行极慢，并且无法与团队共享。
- ❌ **AOP 框架 (如 PostSharp)** 对于简单的调试任务来说太重、编译太慢且显得大材小用。

**CollectionSpy** 通过声明式的 Fluent API 完美解决了这个问题。它提供了标准集合的替换品（如 `TrapList`、`TrapDictionary`），允许你在满足特定数据条件时精准注入 **断点 (Breakpoints)**、**堆栈打印 (Stack Traces)** 或 **自定义日志 (Logs)**。

---

## 📦 安装

通过 NuGet 安装最新版本：

```bash
dotnet add package CollectionSpy
```

或者使用包管理器控制台：
```powershell
Install-Package CollectionSpy
```

---

## 🎮 可视化 Dashboard 演示 (WPF)

想看看 CollectionSpy 的实际效果吗？我们编写了一个实时的 **WPF PLC 信号监控看板**，用于演示如何将 `TrapList` 结合 UI 数据绑定 (`INotifyCollectionChanged`) 一起使用。

*(注：你可以稍后运行 WPF 程序，截取一张动图或截图放在这里)*

1. 克隆仓库。
2. 将 `TrapLibrary.WpfDemo` 设为启动项目。
3. 运行程序，点击 **"🚨 Add Overheat Signal (Trap!)"** 按钮，观察右侧日志是如何瞬间被陷阱捕获并渲染的！

---

## ⚡ 快速开始

### 1. 监控 List

你可以直接实例化 `TrapList`，或使用优雅的扩展方法转换现有的 `IEnumerable`：

```csharp
using Debugging.Traps;
using Debugging.Traps.Extensions; // 引入 .ToTrapList(), .ToTrapDictionary() 等扩展

// 不要使用 new List<User>()，改为这样：
var users = GetUsers().ToTrapList(); 

// 🎯 场景 1: 当添加了 Name 为 null 的非法用户时，直接触发调试器断点
users.OnAdd()
     .When(u => u.Name == null) // 触发条件
     .Do(TrapActions.Break());  // 执行动作 (Debugger.Break)

// 🎯 场景 2: 当关键 ID (如 999) 被移除时记录日志
users.OnRemove()
     .When(u => u.Id == 999)
     .Do(TrapActions.Log("🚨 警告: 管理员 999 被移除了！"));
```

### 2. 监控 Dictionary

非常适合监控配置变更或缓存层：

```csharp
var config = GetConfigs().ToTrapDictionary();

// 🚨 警告: 如果安全配置被降级为不安全的 HTTP
config.OnUpdate()
      .When((key, value) => key == "ApiUrl" && value.StartsWith("http:"))
      .Do(TrapActions.Log("安全警告: API URL 被降级为不安全的 HTTP！"));
```

### 3. 监控 HashSet

轻松捕捉低效的代码逻辑或重复项：

```csharp
var uniqueTags = new TrapHashSet<string>();

// 🐌 发现低效代码: 尝试添加已经存在的标签
uniqueTags.OnAdd()
          .When(tag => uniqueTags.Contains(tag))
          .Do(TrapActions.Log("低效代码: 标签已存在于 Set 中！"));
```

### 4. 监控 Queue 和 Stack

```csharp
// 监控 Queue (先进先出) - 适合任务处理追踪
var jobQueue = new List<string> { "init_job" }.ToTrapQueue();

jobQueue.OnEnqueue()
        .When(job => job == "POISON_PILL")
        .Do(TrapActions.Log("严重: 毒药任务已入队！"));

// 监控 Stack (后进先出) - 适合 UI 导航追踪
var navStack = new TrapStack<string>();

navStack.OnPop()
        .When(page => page == "Root")
        .Do(TrapActions.DumpStackTrace("Root 页面被谁弹出了:"));
```

---

## 🛡️ 性能与生产环境安全性

**CollectionSpy** 专为严苛的生产环境调试而设计，在保证功能的同时将性能开销降到了最低。

### ⚡ 零分配架构 (v1.0+)
底层使用了高度优化的 **Copy-On-Write (写时复制)** 策略来存储规则。
- **零分配 (Zero Allocations)**: 在热路径上（执行添加/移除项时），内存分配为 **0 字节**。在遍历期间不会产生闭包或隐藏对象。
- **微秒级开销**: 即使在陷阱处于激活状态并评估条件时，开销也仅在个位数微秒级别。
- **线程安全的配置**: 规则的添加和移除是无锁且线程安全的。

### 📊 基准测试片段
执行 `List<int>.Add()` 操作 10,000 次的对比：

| 方法 | 平均耗时 | 倍数 | Gen0 内存分配 |
| :--- | :--- | :--- | :--- |
| **原生 `List<T>`** | ~7.9 μs | 1.0x | - |
| **`AddWithoutTrap`** | ~9.7 μs | 1.2x | - |
| **`TrapList` (激活态)** | ~71.0 μs | 8.9x | - |

> *测试环境: AMD Ryzen 9 8945HX, .NET 8.0。完整报告见 [BENCHMARKS.md](../BENCHMARKS.md)。*

### 🚀 绕过陷阱 (批量插入)
如果需要初始化大型集合且不希望触发陷阱和降低性能？请使用 **Bypass API**：

```csharp
// 快速批量插入，0 陷阱开销
myTrapList.AddWithoutTrap(newItem);
myTrapList.AddRange(largeCollection); // 原生速度
```

### 🎛️ 全局安全开关
在生产环境中想要彻底关闭所有开销，只需拨动全局开关：

```csharp
TrapManager.Enabled = false;
```
关闭后，所有拦截逻辑将立即返回（Fast-Fail），开销可以忽略不计。请注意，在 **Release** 模式下，如果陷阱保持激活，库将在启动时发出一次 **控制台警告**。

---

## 🗺️ 路线图与下一步计划

我们正在积极将 CollectionSpy 从一个“好用的工具”进化为“工业级框架”。查看我们的 [Roadmap](ROADMAP.md) 了解即将推出的功能：
- 🚀 支持 `INotifyCollectionChanged`，完美兼容 WPF/WinForms 数据绑定。
- 🏭 支持线程安全的 `ConcurrentDictionary` 和 `ConcurrentQueue`。
- ⚡ 引入 **Source Generators (源生成器)**，实现真正的零运行时开销，对 AOT 更加友好。

## 🤝 参与贡献

欢迎提交 Issue 和 Feature Request！
请访问 [issues 页面](https://github.com/angleyanalbedo/CollectionSpy/issues)。

## 📝 开源协议

本项目基于 [MIT](LICENSE) 协议开源。

---
*由 [angleyanalbedo](https://github.com/angleyanalbedo) 用 ❤️ 制作*