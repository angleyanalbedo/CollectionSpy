# CollectionSpy 🕵️‍♂️

**CollectionSpy** allows you to "trap" and debug hidden modifications in your C# lists and dictionaries. 

It provides a specialized `TrapList<T>` and `TrapDictionary<TKey, TValue>` that act as drop-in replacements for standard collections, enabling you to inject **Breakpoints**, **Stack Traces**, or **Logs** when specific data conditions are met (e.g., "Who added a null ID to this list?").

## 🚀 Why CollectionSpy?

Debugging legacy code or complex state management can be a nightmare when you don't know *who* modified a collection. 
- **Standard `ObservableCollection`** is too verbose for debugging (requires event handlers).
- **Conditional Breakpoints** in IDEs are slow and can't be shared with the team.
- **AOP Frameworks** are overkill for simple debugging tasks.

**CollectionSpy** solves this with a fluent, declarative API.

## 📦 Installation

```bash
dotnet add package CollectionSpy
```

## ⚡ Quick Start

### 1. Spy on a List

You can create a `TrapList` directly, or convert an existing `IEnumerable` using fluent extensions:

```csharp
using Debugging.Traps;
using Debugging.Traps.Extensions; // Import for .ToTrapList()

var users = GetUsers().ToTrapList(); // Convert directly!

// 1. Break execution when a user with null name is added
users.OnAdd()
     .When(u => u.Name == null)
     .Do(TrapActions.Break());

// 2. Log a warning when a specific ID is removed
users.OnRemove()
     .When(u => u.Id == 999)
     .Do(TrapActions.Log("WARNING: Admin user 999 was removed!"));
```

### 2. Spy on a Dictionary

```csharp
var config = GetConfigs().ToTrapDictionary();

// Alert if a secure setting is downgraded to HTTP
config.OnUpdate()
      .When((key, value) => key == "ApiUrl" && value.StartsWith("http:"))
      .Do(TrapActions.Log("SECURITY ALERT: API URL set to insecure HTTP!"));
```

### 3. Spy on a HashSet

```csharp
var uniqueTags = new TrapHashSet<string>();

// Detect inefficient code: adding a tag that already exists
uniqueTags.OnAdd()
          .When(tag => uniqueTags.Contains(tag))
          .Do(TrapActions.Log("Inefficient Code: Tag already exists!"));
```

### 4. Spy on Queue and Stack

```csharp
// Spy on a Queue (FIFO)
var jobQueue = new List<string> { "init_job" }.ToTrapQueue();

jobQueue.OnEnqueue()
        .When(job => job == "POISON_PILL")
        .Do(TrapActions.Log("Critical: Poison pill enqueued!"));

// Spy on a Stack (LIFO)
var navStack = new TrapStack<string>();

navStack.OnPop()
        .When(page => page == "Root")
        .Do(TrapActions.DumpStackTrace("Root Page Popped By"));
```

## 🛡️ Performance & Production Safety

**CollectionSpy** is designed to be safe.

1.  **Zero-Overhead in Release (Default)**: 
    The library includes a global switch `TrapManager.Enabled`.
    - **DEBUG builds**: Enabled by default.
    - **RELEASE builds**: **Disabled** by default. When disabled, the interception logic returns immediately (fast-fail), imposing negligible performance overhead.

2.  **Global Toggle**:
    You can manually control it in your startup logic:
    ```csharp
    // Force enable in production for emergency diagnostics
    TrapManager.Enabled = true; 
    ```

3.  **Thread Safety**: 
    Trap configuration is thread-safe. Execution logic snapshots rules to prevent concurrency issues during enumeration.

## 📝 License

MIT License. See [LICENSE](LICENSE) for details.

---
*Maintained by [angleyanalbedo](https://github.com/angleyanalbedo)*
