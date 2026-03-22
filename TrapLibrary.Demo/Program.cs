using System;
using System.Collections.Generic;
using Debugging.Traps;
using Debugging.Traps.Extensions; // Import Extensions

namespace Debugging.Traps.Demo
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsAdmin { get; set; }

        public override string ToString() => $"{Id}:{Name}";
    }

    class Program
    {
        static void Main(string[] args)
        {
            // 0. Global Configuration (Default: Enabled in DEBUG, Disabled in RELEASE)
            // TrapManager.Enabled = true; // Force enable in production if needed

            Console.WriteLine($"[TrapManager] Status: {(TrapManager.Enabled ? "Enabled" : "Disabled")}");
            
            Console.WriteLine("=== TrapList Demo ===");

            // 1. Create monitored List using Extension Method!
            var initialUsers = new List<User> { new User { Id = 0, Name = "Root" } };
            var userList = initialUsers.ToTrapList(); // <--- Clean syntax

            // 2. Configure Traps (Fluent API)
            
            // Scenario A: Detect null names
            userList.OnAdd()
                    .When(u => u.Name == null)
                    .Do(() => {
                        Console.WriteLine("!!! WARNING: Attempted to add user with null name!");
                        // TrapActions.DumpStackTrace("  -> Source").Invoke();
                    });

            // Scenario B: Monitor specific ID removal
            userList.OnRemove()
                    .When(u => u.Id == 999)
                    .Do(TrapActions.Log("WARNING: Admin account (ID 999) was removed!"));

            // Scenario C: Monitor Clear
            userList.OnClear()
                    .Do(() => Console.WriteLine("List Cleared!"));

            // 3. Simulate operations
            Console.WriteLine("\n--- Normal Operations ---");
            userList.Add(new User { Id = 1, Name = "Alice" }); 

            Console.WriteLine("\n--- Simulate Upcast to IList<T> (Verify Robustness) ---");
            IList<User> legacyInterface = userList;
            
            // Trap works even via interface
            legacyInterface.Add(new User { Id = 2, Name = "Bob" }); 

            Console.WriteLine("\n--- Trigger Scenario A (Add Bad Data) ---");
            legacyInterface.Add(new User { Id = 3, Name = null }); 

            Console.WriteLine("\n--- Trigger Scenario B (Remove Admin) ---");
            var admin = new User { Id = 999, Name = "Admin" };
            userList.Add(admin);
            legacyInterface.Remove(admin); 

            
            Console.WriteLine("\n=============================");
            Console.WriteLine("=== TrapDictionary Demo ===");

            // 4. Create monitored Dictionary
            var configCache = new TrapDictionary<string, string>();

            // Config: Warn if "ApiUrl" is changed to HTTP
            configCache.OnUpdate()
                       .When((key, val) => key == "ApiUrl" && val.StartsWith("http:"))
                       .Do(TrapActions.Log("SECURITY ALERT: ApiUrl downgraded to HTTP!"));

            // Config: Log if long key added
            configCache.OnAdd()
                       .WhenKey(k => k.Length > 10)
                       .Do(() => Console.WriteLine("Note: Long key added."));

            IDictionary<string, string> iDict = configCache;

            iDict.Add("Short", "Val"); 
            iDict.Add("VeryLongConfigurationKey", "Val"); // Triggers WhenKey

            iDict["ApiUrl"] = "https://api.com"; // Add operation, normal
            
            Console.WriteLine("\n=============================");
            Console.WriteLine("=== TrapHashSet Demo ===");

            var activeTags = new TrapHashSet<string>();

            // Config: Detect duplicate add attempts
            // Note: HashSet naturally handles duplicates, but we might want to know WHO is adding them unnecessarily
            activeTags.OnAdd()
                      .When(tag => activeTags.Contains(tag))
                      .Do(TrapActions.Log("Inefficient Code: Tag already exists!"));

            activeTags.Add("urgent");
            Console.WriteLine("Adding 'urgent' again...");
            activeTags.Add("urgent"); // Triggers Log

            
            Console.WriteLine("\n=============================");
            Console.WriteLine("=== TrapQueue Demo ===");

            var jobQueue = new TrapQueue<string>();
            
            // Monitor who is adding "POISON_PILL"
            jobQueue.OnEnqueue()
                    .When(msg => msg == "POISON_PILL")
                    .Do(TrapActions.Log("Critical: Poison pill enqueued!"));

            jobQueue.Enqueue("job_1");
            jobQueue.Enqueue("POISON_PILL"); // Triggers Log

            
            Console.WriteLine("\n=============================");
            Console.WriteLine("=== TrapStack Demo ===");
            
            var navStack = new TrapStack<string>();

            // Monitor stack underflow risk (or just debug state)
            navStack.OnPop()
                    .When(page => page == "Home")
                    .Do(() => Console.WriteLine("Navigating back from Home (Root)..."));

            navStack.Push("Home");
            navStack.Push("Settings");
            navStack.Pop(); // Settings
            navStack.Pop(); // Home -> Triggers console write
        }
    }
}
