using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Utils;
using PhantombiteCreatures.Modules;

namespace PhantombiteCreatures.Core
{
    /// <summary>
    /// Phantombite_Creatures Session — Core v2.0.0 Anbindung
    ///
    /// Protokoll (Mod → Core, Kanal 1995000):
    ///   REGISTER|creatures|Phantombite Creatures|1.0.0|1995003|cmd1:bool:desc|...
    ///   HEAVY_START|creatures|opName
    ///   HEAVY_END|creatures|opName
    ///   PERFACK|creatures|confirmedLevel
    ///   CMDRESULT|creatures|cmd|args|steamId|ok|text
    ///
    /// Protokoll (Core → Mod, Kanal 1995003):
    ///   READY
    ///   LOGLEVEL|0/1/2
    ///   PERFLEVEL|0-3
    ///   CMD|commandName|arg1|...|STEAM:steamId
    /// </summary>
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class Creatures_Session : MySessionComponentBase
    {
        private const string SRC          = "Creatures_Session";
        private const string VERSION      = "1.0.0";
        private const string MOD_NAME     = "creatures";
        private const string MOD_DESC     = "Phantombite Creatures";
        private const long   MY_CHANNEL   = 1995003L;
        private const long   CORE_CHANNEL = 1995000L;

        // Fallback: Init auch ohne Core nach 10 Sekunden
        private const int FALLBACK_TICKS = 600;

        private Creatures_SpawnManager _spawnManager;
        private bool _initialized  = false;
        private bool _coreReady    = false;
        private int  _debugLevel   = 0;   // 0=INFO 1=DEBUG 2=VERBOSE
        private int  _perfLevel    = 0;   // 0=voll 1=reduziert 2=minimal 3=aus
        private int  _fallbackTick = 0;

        // ── LoadData ──────────────────────────────────────────────────────────

        public override void LoadData()
        {
            if (!MyAPIGateway.Multiplayer.IsServer) return;
            try
            {
                MyAPIGateway.Utilities.RegisterMessageHandler(MY_CHANNEL, OnCoreMessage);
                Log("LoadData — warte auf Core READY");
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLineAndConsole("[PB.creatures] [ERROR] LoadData: " + ex);
            }
        }

        // ── Core Kommunikation ────────────────────────────────────────────────

        private void OnCoreMessage(object data)
        {
            try
            {
                string msg = data as string;
                if (string.IsNullOrEmpty(msg)) return;

                // READY — Core ist bereit
                if (msg == "READY")
                {
                    _coreReady = true;
                    SendRegister();
                    Log("READY empfangen — REGISTER gesendet");
                    if (!_initialized) Init();
                    return;
                }

                // LOGLEVEL|0/1/2
                if (msg.StartsWith("LOGLEVEL|"))
                {
                    int level;
                    if (int.TryParse(msg.Substring(9), out level))
                    {
                        _debugLevel = level;
                        ApplyLogLevel(level);
                        Log("LOGLEVEL gesetzt: " + level);
                    }
                    return;
                }

                // PERFLEVEL|0-3
                if (msg.StartsWith("PERFLEVEL|"))
                {
                    int level;
                    if (int.TryParse(msg.Substring(10), out level))
                    {
                        int old = _perfLevel;
                        _perfLevel = level;
                        OnPerfLevelChanged(old, level);
                        // PERFACK zurück an Core
                        MyAPIGateway.Utilities.SendModMessage(CORE_CHANNEL,
                            "PERFACK|" + MOD_NAME + "|" + level);
                        Log("PERFLEVEL: " + old + " → " + level + " (ACK gesendet)");
                    }
                    return;
                }

                // CMD|commandName|arg1|...|STEAM:steamId
                if (msg.StartsWith("CMD|"))
                {
                    HandleCommand(msg);
                    return;
                }
            }
            catch (Exception ex)
            {
                LogError("OnCoreMessage", ex);
            }
        }

        private void SendRegister()
        {
            // Format: REGISTER|name|desc|version|channel|cmd:adminOnly:desc|...
            // Command-Namen dürfen keine Leerzeichen enthalten — Args kommen separat
            string msg =
                "REGISTER|" + MOD_NAME + "|" + MOD_DESC + "|" + VERSION + "|" + MY_CHANNEL +
                "|status:false:Aktive Kreaturen und Wellen anzeigen" +
                "|spawn:true:Spawn-Timer zurücksetzen. Arg: wolf / spider / spiderbrown / spiderblack" +
                "|timer:true:Timer für Spieler zurücksetzen. Arg: <Spielername>";
            MyAPIGateway.Utilities.SendModMessage(CORE_CHANNEL, msg);
        }

        // ── Performance ───────────────────────────────────────────────────────

        private void OnPerfLevelChanged(int oldLevel, int newLevel)
        {
            // Level 0 = volle Spawn-Rate (Normal)
            // Level 1 = reduzierte Rate (Spawn-Intervall verdoppelt in Update)
            // Level 2 = minimal (kein automatischer Spawn, nur Force-Spawn via Command)
            // Level 3 = komplett aus (kein Update)
            Log("Performance Level: " + oldLevel + " → " + newLevel);
        }

        // ── Command Handling ──────────────────────────────────────────────────

        private void HandleCommand(string msg)
        {
            try
            {
                // Format von Core: CMD|commandName|arg1|...|STEAM:steamId
                string[] parts = msg.Split('|');
                if (parts.Length < 3) return;

                string commandName = parts[1].ToLower().Trim();
                string steamPart   = parts[parts.Length - 1]; // "STEAM:76561198..."
                string steamId     = steamPart.StartsWith("STEAM:") ? steamPart.Substring(6) : "0";
                ulong  steamUlong;
                ulong.TryParse(steamId, out steamUlong);

                // Args: alles zwischen commandName und STEAM
                string argsJoined = "";
                if (parts.Length > 3)
                {
                    var argList = new List<string>();
                    for (int i = 2; i < parts.Length - 1; i++)
                    {
                        if (!parts[i].StartsWith("STEAM:")) argList.Add(parts[i]);
                    }
                    argsJoined = string.Join("|", argList);
                }

                string arg1 = argsJoined.ToLower().Trim();

                string resultText;
                bool   ok;

                switch (commandName)
                {
                    case "status":
                        resultText = _spawnManager != null ? _spawnManager.GetStatus() : "SpawnManager nicht bereit";
                        ok = true;
                        break;

                    case "spawn":
                        if (string.IsNullOrEmpty(arg1))
                        {
                            _spawnManager?.ForceSpawn(steamUlong);
                            resultText = "Spawn-Timer zurückgesetzt — Kreaturen erscheinen in ~5s";
                            ok = true;
                        }
                        else
                        {
                            string subtype = MapCreatureArg(arg1);
                            if (subtype == null)
                            {
                                resultText = "Unbekannter Typ: " + arg1 + " (wolf / spider / spiderbrown / spiderblack)";
                                ok = false;
                            }
                            else
                            {
                                _spawnManager?.ForceSpawnType(steamUlong, subtype);
                                resultText = "Spawn: " + subtype;
                                ok = true;
                            }
                        }
                        break;

                    case "timer":
                        if (string.IsNullOrEmpty(arg1))
                        {
                            _spawnManager?.ForceSpawn(steamUlong);
                            resultText = "Timer zurückgesetzt für dich";
                            ok = true;
                        }
                        else
                        {
                            resultText = ResetTimerForPlayer(arg1, steamUlong);
                            ok = resultText != null;
                            if (!ok) resultText = "Spieler nicht gefunden: " + arg1;
                        }
                        break;

                    default:
                        resultText = "Commands: status | spawn | spawn wolf/spider | timer <name>";
                        ok = false;
                        break;
                }

                SendCmdResult(commandName, argsJoined, steamId, ok, resultText);
            }
            catch (Exception ex)
            {
                LogError("HandleCommand", ex);
            }
        }

        private void SendCmdResult(string cmd, string args, string steamId, bool ok, string result)
        {
            // Format: CMDRESULT|modName|commandName|argsJoined|steamId|status|resultMessage
            string msg = "CMDRESULT|" + MOD_NAME + "|" + cmd + "|" + args +
                         "|" + steamId + "|" + (ok ? "ok" : "error") + "|" + result;
            MyAPIGateway.Utilities.SendModMessage(CORE_CHANNEL, msg);
        }

        private string ResetTimerForPlayer(string nameArg, ulong requesterId)
        {
            var players = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(players);
            foreach (var p in players)
            {
                if (!p.DisplayName.Equals(nameArg, StringComparison.OrdinalIgnoreCase)) continue;
                _spawnManager?.ForceSpawn(p.SteamUserId);
                return "Timer zurückgesetzt für: " + p.DisplayName;
            }
            return null;
        }

        private string MapCreatureArg(string arg)
        {
            switch (arg)
            {
                case "wolf":         return "Wolf";
                case "spider":       return "SpaceSpider";
                case "spiderbrown":  return "SpaceSpiderBrown";
                case "spiderblack":  return "SpaceSpiderBlack";
                default:             return null;
            }
        }

        // ── HEAVY Callbacks (weitergeleitet an Core) ──────────────────────────

        private void HeavyStart(string opName)
        {
            MyAPIGateway.Utilities.SendModMessage(CORE_CHANNEL,
                "HEAVY_START|" + MOD_NAME + "|" + opName);
        }

        private void HeavyEnd(string opName)
        {
            MyAPIGateway.Utilities.SendModMessage(CORE_CHANNEL,
                "HEAVY_END|" + MOD_NAME + "|" + opName);
        }

        // ── Update ────────────────────────────────────────────────────────────

        public override void UpdateBeforeSimulation()
        {
            try
            {
                if (!MyAPIGateway.Multiplayer.IsServer) return;

                // Fallback-Init wenn Core nicht antwortet
                if (!_initialized)
                {
                    _fallbackTick++;
                    if (_fallbackTick >= FALLBACK_TICKS)
                    {
                        Log("Fallback-Init (kein Core READY nach 10s)");
                        Init();
                    }
                    return;
                }

                // PerfLevel 3 = komplett aus
                if (_perfLevel >= 3) return;

                // PerfLevel 1/2 = reduzierte Rate (jeden 2. Tick überspringen)
                if (_perfLevel >= 1 && (MyAPIGateway.Session.GameplayFrameCounter % 2 != 0)) return;

                _spawnManager?.Update();
                _spawnManager?.ProcessSuppressQueue();
            }
            catch (Exception ex)
            {
                LogError("UpdateBeforeSimulation", ex);
            }
        }

        // ── Init ──────────────────────────────────────────────────────────────

        private void Init()
        {
            try
            {
                // Logger instanziieren (Singleton — muss vor allen anderen Calls erstellt werden)
                new Creatures_Logger();

                Log("Initialisierung gestartet");
                var definitions = Creatures_FileManager.Load();
                _spawnManager = new Creatures_SpawnManager();

                // HEAVY Callbacks registrieren
                _spawnManager.OnHeavyStart = HeavyStart;
                _spawnManager.OnHeavyEnd   = HeavyEnd;

                _spawnManager.Init(definitions);
                _initialized = true;
                Log("Initialisierung abgeschlossen — " + definitions.Count + " Definitionen");
            }
            catch (Exception ex)
            {
                LogError("Init", ex);
            }
        }

        // ── Unload ────────────────────────────────────────────────────────────

        protected override void UnloadData()
        {
            try
            {
                if (MyAPIGateway.Utilities != null)
                    MyAPIGateway.Utilities.UnregisterMessageHandler(MY_CHANNEL, OnCoreMessage);
                _spawnManager?.Close();
                Creatures_Logger.Instance?.Close();
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLineAndConsole("[PB.creatures] [ERROR] UnloadData: " + ex);
            }
        }

        // ── Logging ───────────────────────────────────────────────────────────

        private void ApplyLogLevel(int level)
        {
            // Creatures_Logger auf neues Level setzen
            if (Creatures_Logger.Instance == null) return;
            switch (level)
            {
                case 0: Creatures_Logger.Instance.SetLogLevel("normal"); break;
                case 1: Creatures_Logger.Instance.SetLogLevel("debug");  break;
                case 2: Creatures_Logger.Instance.SetLogLevel("trace");  break;
            }
        }

        private void Log(string msg)
        {
            MyLog.Default.WriteLineAndConsole("[PB.creatures] " + SRC + ": " + msg);
        }

        private void LogError(string context, Exception ex)
        {
            MyLog.Default.WriteLineAndConsole("[PB.creatures] [ERROR] " + context + ": " + ex);
        }
    }
}