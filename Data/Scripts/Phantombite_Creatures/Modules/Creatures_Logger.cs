using System;
using Sandbox.ModAPI;
using VRage.Utils;

namespace PhantombiteCreatures.Modules
{
    /// <summary>
    /// Creatures_Logger — schreibt in SE-Log UND leitet an Core weiter.
    ///
    /// Core-Protokoll (Kanal 1995000):
    ///   LOG|creatures|level|module|message
    ///   level: 0 = Info/Warn/Error, 1 = Debug, 2 = Trace
    /// </summary>
    public class Creatures_Logger
    {
        public static Creatures_Logger Instance;

        public enum LogLevel { Normal, Debug, Trace }

        private LogLevel _level = LogLevel.Normal;

        // Muss dem Namen entsprechen unter dem Core den Debug-Level speichert
        private const string MOD_NAME    = "Phantombite_Creatures";
        private const long   CORE_CHANNEL = 1995000L;
        private const long   LOG_CHANNEL  = 1995999L;

        public Creatures_Logger()
        {
            Instance = this;
        }

        public void SetLogLevel(string level)
        {
            switch (level.ToLower())
            {
                case "debug": _level = LogLevel.Debug; break;
                case "trace": _level = LogLevel.Trace; break;
                default:      _level = LogLevel.Normal; break;
            }
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void Info(string src, string msg)
        {
            Write(0, src, msg);
        }

        public void Warn(string src, string msg)
        {
            Write(0, src, "[WARN] " + msg);
        }

        public void Error(string src, string msg)
        {
            Write(0, src, "[ERROR] " + msg);
        }

        public void Debug(string src, string msg)
        {
            if (_level < LogLevel.Debug) return;
            Write(1, src, msg);
        }

        public void Trace(string src, string msg)
        {
            if (_level < LogLevel.Trace) return;
            Write(2, src, msg);
        }

        // ── Intern ───────────────────────────────────────────────────────────

        private void Write(int level, string src, string msg)
        {
            try
            {
                // 1. SE-Log (immer, sofort)
                string line = string.Format("[Phantombite_Creatures] [{0}] [{1}] {2}",
                    LevelTag(level), src, msg);
                MyLog.Default.WriteLineAndConsole(line);

                // 2. Core LOG-Protokoll → Phantombite-Log-Datei (Kanal 1995999)
                // Format: LOG|modName|level|module|message
                if (MyAPIGateway.Utilities != null)
                {
                    string coreMsg = "LOG|" + MOD_NAME + "|" + level + "|" + src + "|" + msg;
                    MyAPIGateway.Utilities.SendModMessage(LOG_CHANNEL, coreMsg);
                }
            }
            catch { }
        }

        private static string LevelTag(int level)
        {
            switch (level)
            {
                case 0: return "INFO";
                case 1: return "DEBUG";
                case 2: return "TRACE";
                default: return "INFO";
            }
        }

        public void Close()
        {
            Instance = null;
        }
    }
}