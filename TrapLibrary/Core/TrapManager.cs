using System;
using System.Diagnostics;
using System.Reflection;

namespace Debugging.Traps
{
    /// <summary>
    /// Global configuration and control for all Trap collections.
    /// </summary>
    public static class TrapManager
    {
        private static bool _enabled = true;

        /// <summary>
        /// Global switch to enable or disable all traps.
        /// Default is TRUE for all builds (Debug & Release).
        /// </summary>
        public static bool Enabled
        {
            get => _enabled;
            set
            {
                _enabled = value;
            }
        }

        static TrapManager()
        {
            ShowPerformanceHint();
        }

        /// <summary>
        /// Check if the entry assembly (the main application) is built in Debug mode.
        /// </summary>
        private static bool IsDebugBuild
        {
            get
            {
                var assembly = Assembly.GetEntryAssembly();
                if (assembly == null) return false;

                var debuggableAttribute = assembly.GetCustomAttribute<DebuggableAttribute>();
                return debuggableAttribute != null && debuggableAttribute.IsJITTrackingEnabled;
            }
        }

        private static void ShowPerformanceHint()
        {
            // Use runtime check instead of compile-time #if DEBUG
            if (!IsDebugBuild)
            {
                try
                {
                    var originalColor = Console.ForegroundColor;
                    if (_enabled)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("[CollectionSpy] ⚠️ WARNING: Traps are enabled in Release mode. Disable them for best performance.");
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("[CollectionSpy] ℹ️ Traps are disabled for best performance.");
                    }
                    Console.ForegroundColor = originalColor;
                }
                catch
                {
                    // Fallback for headless environments (e.g. no console attached)
                    System.Diagnostics.Debug.WriteLine(_enabled 
                        ? "[CollectionSpy] ⚠️ WARNING: Traps are enabled in Release mode. Disable them for best performance." 
                        : "[CollectionSpy] ℹ️ Traps are disabled for best performance.");
                }
            }
        }
    }
}
