using System;

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

        private static void ShowPerformanceHint()
        {
#if !DEBUG
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
#endif
        }
    }
}
