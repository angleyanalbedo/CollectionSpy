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

Replace `new List<User>()` with `new TrapList<User>()`. It is fully compatible with `IList<T>`.

```csharp
using Debugging.Traps;

var users = new TrapList<User>();

// 1. Break execution when a user with null name is added
users.OnAdd()
     .When(u => u.Name == null)
     .Do(TrapActions.Break());

// 2. Log a warning when a specific ID is removed
users.OnRemove()
     .When(u => u.Id == 999)
     .Do(TrapActions.Log("WARNING: Admin user 999 was removed!"));

// 3. Print Stack Trace when the list is cleared
users.OnClear()
     .Do(TrapActions.DumpStackTrace("List Cleared By"));
```

### 2. Spy on a Dictionary

Replace `new Dictionary<K, V>()` with `new TrapDictionary<K, V>()`.

```csharp
var config = new TrapDictionary<string, string>();

// Alert if a secure setting is downgraded to HTTP
config.OnUpdate()
      .When((key, value) => key == "ApiUrl" && value.StartsWith("http:"))
      .Do(TrapActions.Log("SECURITY ALERT: API URL set to insecure HTTP!"));

// Filter by Key only
config.OnAdd()
      .WhenKey(k => k.Length > 50)
      .Do(() => Console.WriteLine("Performance Warning: Huge key added."));
```

### 3. Spy on a HashSet

Replace `new HashSet<T>()` with `new TrapHashSet<T>()`.

```csharp
var uniqueTags = new TrapHashSet<string>();

// Detect inefficient code: adding a tag that already exists
uniqueTags.OnAdd()
          .When(tag => uniqueTags.Contains(tag))
          .Do(TrapActions.Log("Inefficient Code: Tag already exists!"));

// Break when a critical permission is removed
uniqueTags.OnRemove()
          .When(tag => tag == "SUPER_ADMIN")
          .Do(TrapActions.Break());

### 4. Spy on Queue and Stack

```csharp
var jobQueue = new TrapQueue<string>();

// Debug who added a "Poison Pill" message
jobQueue.OnEnqueue()
        .When(msg => msg == "POISON_PILL")
        .Do(TrapActions.Break());

// ---

var navStack = new TrapStack<string>();

// Log when the root page is popped
navStack.OnPop()
        .When(page => page == "RootPage")
        .Do(TrapActions.DumpStackTrace("Root Popped By"));
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
