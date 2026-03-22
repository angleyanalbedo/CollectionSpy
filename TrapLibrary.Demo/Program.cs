using System;
using System.Collections.Generic;
using Debugging.Traps;

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

            // 1. Create monitored List
            var userList = new TrapList<User>();

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
            
            Console.WriteLine("\n--- Trigger Dictionary Update Trap ---");
            iDict["ApiUrl"] = "http://insecure.com"; // Update operation, triggers Log
        }
    }
}
