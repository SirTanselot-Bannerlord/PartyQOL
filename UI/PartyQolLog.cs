using System;
using System.IO;

namespace PartyQOL.UI
{
    /// <summary>Append-only trace next to PartyQOL.dll. Swallows IO failures.</summary>
    internal static class PartyQolLog
    {
        internal static void Write(string message)
        {
            try
            {
                string dir = Path.GetDirectoryName(typeof(PartyQolLog).Assembly.Location);
                if (string.IsNullOrEmpty(dir))
                {
                    return;
                }

                string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " " + message + Environment.NewLine;
                File.AppendAllText(Path.Combine(dir, "PartyQOL.log"), line);
            }
            catch
            {
                // Logging must not take the game down.
            }
        }

        internal static void Write(string message, Exception exception)
        {
            Write(message + " " + exception);
        }
    }
}
