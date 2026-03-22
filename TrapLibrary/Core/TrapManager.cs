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
        /// Default is true in DEBUG builds, false in RELEASE builds.
        /// </summary>
        public static bool Enabled { get; set; }

        static TrapManager()
        {
#if DEBUG
            Enabled = true;
#else
            Enabled = false;
#endif
        }
    }
}
