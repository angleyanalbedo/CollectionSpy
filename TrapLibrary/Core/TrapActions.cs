using System;
using System.Diagnostics;

namespace Debugging.Traps
{
    /// <summary>
    /// Provides out-of-the-box debugging actions.
    /// </summary>
    public static class TrapActions
    {
        public static Action<string> LogAction { get; set; } = msg => Console.WriteLine($"[TRAP LOG] {msg}");

        /// <summary>
        /// Trigger a debugger break.
        /// </summary>
        public static Action Break()
        {
            return () =>
            {
                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }
            };
        }

        /// <summary>
        /// Print the current stack trace.
        /// </summary>
        public static Action DumpStackTrace(string prefix = "")
        {
            return () =>
            {
                var trace = new StackTrace(true);
                LogAction($"{prefix} StackTrace:\n{trace}");
            };
        }

        /// <summary>
        /// Log a simple message.
        /// </summary>
        public static Action Log(string message)
        {
            return () => LogAction(message);
        }
    }
}
