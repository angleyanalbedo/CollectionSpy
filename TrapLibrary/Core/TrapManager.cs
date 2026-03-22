using System;

namespace Debugging.Traps
{
    /// <summary>
    /// Global configuration and control for all Trap collections.
    /// </summary>
    public static class TrapManager
    {
        /// <summary>
        /// Global switch to enable or disable all traps.
        /// Default is TRUE for all builds (Debug & Release).
        /// </summary>
        public static bool Enabled { get; set; } = true;

        static TrapManager()
        {
#if !DEBUG
            // In RELEASE builds, warn the user that performance might be impacted.
            try
            {
                var originalColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("[CollectionSpy] ⚠️ WARNING: Traps are ACTIVE in Release mode. Set TrapManager.Enabled=false to optimize performance.");
                Console.ForegroundColor = originalColor;
            }
            catch
            {
                // Fallback for headless environments (e.g. no console attached)
                System.Diagnostics.Debug.WriteLine("[CollectionSpy] ⚠️ WARNING: Traps are ACTIVE in Release mode.");
            }
#endif
        }
    }
}
