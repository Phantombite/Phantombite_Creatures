using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Utils;

namespace PhantombiteCreatures.Core
{
    /// <summary>
    /// Creatures_Command — Core v2.0.0 Anbindung
    ///
    /// Läuft auf CLIENT und SERVER (kein IsServer Guard in LoadData).
    /// Registriert "creature" beim Core, empfängt CMD und leitet an
    /// Creatures_Session weiter (nur wenn IsServer).
    ///
    /// Commands:
    ///   spawn [typ] [spieler]   — Kreatur spawnen
    ///   status                  — Timer-Status aller Spieler anzeigen
    /// </summary>
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class Creatures_Command : MySessionComponentBase
    {
        private const string SRC          = "Creatures_Command";
        private const string VERSION      = "1.0.0";
        private const string MOD_KEY      = "creatures";
        private const string MOD_DESC     = "Phantombite Creatures";
        private const long   MY_CHANNEL   = 1995003L;
        private const long   CORE_CHANNEL = 1995000L;
        private const long   LOG_CHANNEL  = 1995999L;

        private int _logLevel = 0;

        // Dedup: verhindert dass 4 Handler gleichzeitig CMDRESULT senden
        private static System.DateTime _lastCmdResultSent = System.DateTime.MinValue;
        private static readonly System.TimeSpan DEDUP_WINDOW = System.TimeSpan.FromMilliseconds(500);

        // ── LoadData ──────────────────────────────────────────────────────────
        // Kein IsServer Guard — auf Client UND Server registrieren

        public override void LoadData()
        {
            try
            {
                MyAPIGateway.Utilities.RegisterMessageHandler(MY_CHANNEL, OnCoreMessage);
                Log("LoadData — Handler registriert");
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLineAndConsole("[PB.creature] [ERROR] " + SRC + " LoadData: " + ex);
            }
        }

        protected override void UnloadData()
        {
            try
            {
                if (MyAPIGateway.Utilities != null)
                    MyAPIGateway.Utilities.UnregisterMessageHandler(MY_CHANNEL, OnCoreMessage);
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLineAndConsole("[PB.creature] [ERROR] " + SRC + " UnloadData: " + ex);
            }
        }

        // ── Core Kommunikation ────────────────────────────────────────────────

        private void OnCoreMessage(object data)
        {
            try
            {
                string msg = data as string;
                if (string.IsNullOrEmpty(msg)) return;

                if (msg == "READY")
                {
                    SendRegister();
                    // Session initialisieren (nur auf Server)
                    if (MyAPIGateway.Multiplayer.IsServer)
                        Creatures_Session.Instance?.OnCoreReady();
                    Log("READY empfangen — REGISTER gesendet");
                    return;
                }

                if (msg.StartsWith("LOGLEVEL|"))
                {
                    int level;
                    if (int.TryParse(msg.Substring(9), out level))
                        _logLevel = level;
                    return;
                }

                if (msg.StartsWith("PERFLEVEL|"))
                {
                    int level;
                    if (int.TryParse(msg.Substring(10), out level))
                    {
                        if (MyAPIGateway.Multiplayer.IsServer)
                            Creatures_Session.Instance?.SetPerfLevel(level);
                        MyAPIGateway.Utilities.SendModMessage(CORE_CHANNEL,
                            "PERFACK|" + MOD_KEY + "|" + level);
                    }
                    return;
                }

                if (msg.StartsWith("CMD|"))
                {
                    // Commands nur auf Server ausführen
                    if (!MyAPIGateway.Multiplayer.IsServer) return;
                    HandleCommand(msg);
                    return;
                }
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLineAndConsole("[PB.creature] [ERROR] " + SRC + " OnCoreMessage: " + ex);
            }
        }

        private void SendRegister()
        {
            string msg = "REGISTER|" + MOD_KEY + "|" + MOD_DESC + "|" + VERSION + "|" + MY_CHANNEL
                + "|spawn:1:Kreatur spawnen (!pbc creatures spawn [typ] [spieler])"
                + "|status:0:Spawn-Timer aller Spieler anzeigen";
            MyAPIGateway.Utilities.SendModMessage(CORE_CHANNEL, msg);
        }

        // ── Command Handling ──────────────────────────────────────────────────

        private void HandleCommand(string msg)
        {
            try
            {
                // Format: CMD|commandName|arg1|arg2|...|STEAM:steamId
                string[] parts = msg.Split('|');
                if (parts.Length < 3) return;

                string command = parts[1].ToLower().Trim();

                // SteamId aus letztem Teil extrahieren
                string steamPart = parts[parts.Length - 1];
                ulong  steamId   = 0;
                int    argEnd    = parts.Length;
                if (steamPart.StartsWith("STEAM:"))
                {
                    ulong.TryParse(steamPart.Substring(6), out steamId);
                    argEnd = parts.Length - 1;
                }

                // Args sammeln (zwischen commandName und STEAM)
                var argList = new List<string>();
                for (int i = 2; i < argEnd; i++)
                    if (!string.IsNullOrEmpty(parts[i].Trim()))
                        argList.Add(parts[i].Trim());

                string arg0 = argList.Count > 0 ? argList[0].ToLower() : "";
                string arg1 = argList.Count > 1 ? argList[1]           : "";

                var session = Creatures_Session.Instance;
                if (session == null)
                {
                    SendCmdResult(command, "", steamId, false, "Creatures nicht bereit");
                    return;
                }

                bool   ok     = true;
                string result = "";

                switch (command)
                {
                    case "spawn":
                        result = HandleSpawn(session, arg0, arg1, steamId);
                        break;

                    case "status":
                        try
                        {
                            result = session.GetStatus();
                            if (string.IsNullOrEmpty(result))
                                result = "Keine Daten verfügbar";
                        }
                        catch (Exception statusEx)
                        {
                            result = "Fehler in GetStatus: " + statusEx.Message;
                            ok = false;
                        }
                        break;

                    default:
                        result = "Commands: spawn [typ] [spieler] | status";
                        ok = false;
                        break;
                }

                SendCmdResult(command, string.Join("|", argList), steamId, ok, result);
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLineAndConsole("[PB.creature] [ERROR] " + SRC + " HandleCommand: " + ex);
            }
        }

        private string HandleSpawn(Creatures_Session session, string creatureArg, string playerArg, ulong senderSteamId)
        {
            // Ziel-Spieler bestimmen
            ulong  targetId   = senderSteamId;
            string targetName = GetPlayerName(senderSteamId);

            if (!string.IsNullOrEmpty(playerArg))
            {
                bool found = false;
                var players = new List<IMyPlayer>();
                MyAPIGateway.Players.GetPlayers(players);
                foreach (var p in players)
                {
                    if (p.DisplayName.Equals(playerArg, StringComparison.OrdinalIgnoreCase))
                    {
                        targetId   = p.SteamUserId;
                        targetName = p.DisplayName;
                        found      = true;
                        break;
                    }
                }
                if (!found)
                    return "Spieler nicht gefunden: " + playerArg;
            }

            // Creature-Typ bestimmen
            if (string.IsNullOrEmpty(creatureArg))
            {
                session.ForceSpawn(targetId);
                return "Zufällige Kreatur bei " + targetName + " gespawnt";
            }

            string subtype = MapCreatureArg(creatureArg);
            if (subtype == null)
                return "Unbekannter Typ: " + creatureArg + "\nGültig: wolf | spider | spiderbrown | spiderblack";

            session.ForceSpawnType(targetId, subtype);
            return subtype + " bei " + targetName + " gespawnt";
        }

        private string MapCreatureArg(string arg)
        {
            switch (arg)
            {
                case "wolf":        return "Wolf";
                case "spider":      return "SpaceSpider";
                case "spiderbrown": return "SpaceSpiderBrown";
                case "spiderblack": return "SpaceSpiderBlack";
                default:            return null;
            }
        }

        private string GetPlayerName(ulong steamId)
        {
            var players = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(players);
            foreach (var p in players)
                if (p.SteamUserId == steamId) return p.DisplayName;
            return steamId.ToString();
        }

        private void SendCmdResult(string cmd, string args, ulong steamId, bool ok, string result)
        {
            // Dedup: nur einmal senden, auch wenn mehrere Handler registriert sind
            var now = System.DateTime.UtcNow;
            if (now - _lastCmdResultSent < DEDUP_WINDOW) return;
            _lastCmdResultSent = now;

            string msg = "CMDRESULT|" + MOD_KEY + "|" + cmd + "|" + args
                + "|" + steamId + "|" + (ok ? "ok" : "error") + "|" + result;
            MyAPIGateway.Utilities.SendModMessage(CORE_CHANNEL, msg);
        }

        // ── Logging ───────────────────────────────────────────────────────────

        private void Log(string msg, int level = 0)
        {
            if (level > _logLevel) return;
            MyLog.Default.WriteLineAndConsole("[PB.creature] " + SRC + ": " + msg);
        }
    }
}