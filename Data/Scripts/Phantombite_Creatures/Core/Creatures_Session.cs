using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Utils;
using PhantombiteCreatures.Modules;

namespace PhantombiteCreatures.Core
{
    /// <summary>
    /// Creatures_Session — Spawn-Logik (nur Server)
    ///
    /// Core-Kommunikation (READY, REGISTER, CMD) ist in Creatures_Command.
    /// Diese Session stellt das Public Interface für Creatures_Command bereit:
    ///   - Instance       (statische Referenz)
    ///   - OnCoreReady()  (Init-Trigger)
    ///   - SetPerfLevel() (Performance)
    ///   - GetStatus()    (Timer-Anzeige)
    ///   - ForceSpawn()   (Command-Interface)
    ///   - ForceSpawnType()
    /// </summary>
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class Creatures_Session : MySessionComponentBase
    {
        private const string SRC          = "Creatures_Session";
        private const long   CORE_CHANNEL = 1995000L;

        // Öffentliche Referenz für Creatures_Command
        public static Creatures_Session Instance { get; private set; }

        private Creatures_SpawnManager _spawnManager;
        private bool _initialized  = false;
        private int  _perfLevel    = 0;
        private int  _fallbackTick = 0;
        private const int FALLBACK_TICKS = 600; // 10 Sekunden

        // ── LoadData ──────────────────────────────────────────────────────────

        public override void LoadData()
        {
            if (!MyAPIGateway.Multiplayer.IsServer) return;
            Instance = this;
            Log("LoadData — Server-Instanz bereit");
        }

        // ── Public Interface für Creatures_Command ────────────────────────────

        /// <summary>Wird von Creatures_Command aufgerufen wenn Core READY sendet.</summary>
        public void OnCoreReady()
        {
            if (!_initialized) Init();
        }

        public void SetPerfLevel(int level)
        {
            _perfLevel = level;
            Log("PerfLevel gesetzt: " + level);
        }

        public string GetStatus()
        {
            if (_spawnManager == null) return "SpawnManager nicht bereit";
            return _spawnManager.GetStatus();
        }

        public void ForceSpawn(ulong steamId)
        {
            _spawnManager?.ForceSpawn(steamId);
        }

        public void ForceSpawnType(ulong steamId, string subtype)
        {
            _spawnManager?.ForceSpawnType(steamId, subtype);
        }

        // ── HEAVY Callbacks an Core ───────────────────────────────────────────

        public void HeavyStart(string opName)
        {
            try { MyAPIGateway.Utilities.SendModMessage(CORE_CHANNEL, "HEAVY_START|creatures|" + opName); }
            catch { }
        }

        public void HeavyEnd(string opName)
        {
            try { MyAPIGateway.Utilities.SendModMessage(CORE_CHANNEL, "HEAVY_END|creatures|" + opName); }
            catch { }
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

                // PerfLevel 3 = komplett deaktiviert
                if (_perfLevel >= 3) return;

                // PerfLevel 1/2 = jeden 2. Tick überspringen
                if (_perfLevel >= 1 && (MyAPIGateway.Session.GameplayFrameCounter % 2 != 0)) return;

                _spawnManager?.Update();
                _spawnManager?.ProcessSuppressQueue();
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLineAndConsole("[PB.creature] [ERROR] " + SRC + " Update: " + ex);
            }
        }

        // ── Init ──────────────────────────────────────────────────────────────

        private void Init()
        {
            if (_initialized) return;
            try
            {
                new Creatures_Logger();
                Log("Initialisierung gestartet");

                var definitions = Creatures_FileManager.Load();
                _spawnManager = new Creatures_SpawnManager();
                _spawnManager.OnHeavyStart = HeavyStart;
                _spawnManager.OnHeavyEnd   = HeavyEnd;
                _spawnManager.Init(definitions);

                _initialized = true;
                Log("Initialisierung abgeschlossen — " + definitions.Count + " Definitionen");
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLineAndConsole("[PB.creature] [ERROR] " + SRC + " Init: " + ex);
            }
        }

        // ── Unload ────────────────────────────────────────────────────────────

        protected override void UnloadData()
        {
            try
            {
                _spawnManager?.Close();
                Creatures_Logger.Instance?.Close();
                Instance = null;
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLineAndConsole("[PB.creature] [ERROR] " + SRC + " UnloadData: " + ex);
            }
        }

        // ── Logging ───────────────────────────────────────────────────────────

        private void Log(string msg)
        {
            MyLog.Default.WriteLineAndConsole("[PB.creature] " + SRC + ": " + msg);
        }
    }
}