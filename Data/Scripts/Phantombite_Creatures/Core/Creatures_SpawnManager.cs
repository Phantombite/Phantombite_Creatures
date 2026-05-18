using System;
using System.Collections.Generic;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;
using PhantombiteCreatures.Modules;

namespace PhantombiteCreatures.Core
{
    public class Creatures_SpawnManager
    {
        private const string SRC = "Creatures_SpawnManager";

        // ── Timing ────────────────────────────────────────────────────────────
        private const int SPAWN_INTERVAL_TICKS  = 300;   // Alle 5s prüfen
        private const int DESPAWN_INTERVAL_TICKS = 600;  // Alle 10s Despawn-Check
        private const int DEAD_CHECK_INTERVAL    = 600;  // Alle 10s Leichen-Check
        private const int SPAWN_ATTEMPTS         = 10;
        private const double MAX_SPAWN_ALTITUDE  = 150.0;

        // Wave-System Timing (in Ticks bei 60t/s)
        private const int WAVE_TIMER_MIN = 18000;   // 5 Minuten minimal
        private const int WAVE_TIMER_MAX = 72001;   // 20 Minuten maximal

        // ── Felder ────────────────────────────────────────────────────────────
        private List<CreatureDefinition>                    _definitions;
        private Dictionary<string, IMyEntity>               _planetCache    = new Dictionary<string, IMyEntity>();
        private Dictionary<long, SpawnedCreature>           _spawned        = new Dictionary<long, SpawnedCreature>();
        private Dictionary<string, Dictionary<ulong, int>>  _perPlayerCount = new Dictionary<string, Dictionary<ulong, int>>();
        private HashSet<string>                             _radiationWeather = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Timer pro Spieler — neuer Timer startet erst wenn alle Kreaturen weg sind
        private Dictionary<ulong, int> _playerCooldownTicks = new Dictionary<ulong, int>();
        private HashSet<long>          _vanillaSuppressQueue = new HashSet<long>();

        private static readonly HashSet<string> VANILLA_SUBTYPES = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { "SpaceSpider", "SpaceSpiderBrown", "SpaceSpiderBlack", "Wolf" };

        private int  _spawnTick   = 0;
        private int  _despawnTick = 0;
        private int  _deadTick    = 0;
        private Random _rng = new Random();

        // ── HEAVY Callbacks (von Session gesetzt) ─────────────────────────────
        /// <summary>Wird aufgerufen wenn eine schwere Operation beginnt.</summary>
        public Action<string> OnHeavyStart;
        /// <summary>Wird aufgerufen wenn eine schwere Operation endet.</summary>
        public Action<string> OnHeavyEnd;

        // ── Init / Close ──────────────────────────────────────────────────────

        public void Init(List<CreatureDefinition> definitions)
        {
            _definitions = definitions;
            RefreshPlanetCache();
            ScanRadiationWeather();
            RegisterDamageHandler();

            MyAPIGateway.Entities.OnEntityAdd += OnEntityAdded;
            MyAPIGateway.Session.SessionSettings.EnableWolfs   = false;
            MyAPIGateway.Session.SessionSettings.EnableSpiders = false;

            ScanAndRemoveExistingVanilla();
            Creatures_Logger.Instance?.Info(SRC, "SpawnManager initialisiert — " + _definitions.Count + " Definitionen");
        }

        public void Close()
        {
            MyAPIGateway.Entities.OnEntityAdd -= OnEntityAdded;
            _spawned.Clear();
            _perPlayerCount.Clear();
            _playerCooldownTicks.Clear();
        }

        // ── Update ────────────────────────────────────────────────────────────

        public void Update()
        {
            _spawnTick++;
            _despawnTick++;
            _deadTick++;

            if (_spawnTick   >= SPAWN_INTERVAL_TICKS)  { _spawnTick   = 0; TrySpawnAll();   }
            if (_despawnTick >= DESPAWN_INTERVAL_TICKS) { _despawnTick = 0; DespawnCheck();  }
            if (_deadTick    >= DEAD_CHECK_INTERVAL)    { _deadTick    = 0; DeadCheck();     }
        }

        // ── Vanilla Unterdrückung ─────────────────────────────────────────────

        private void OnEntityAdded(IMyEntity entity)
        {
            try
            {
                var character = entity as IMyCharacter;
                if (character == null) return;
                string subtype = character.Definition?.Id.SubtypeName ?? "";
                if (VANILLA_SUBTYPES.Contains(subtype) && !_spawned.ContainsKey(entity.EntityId))
                    _vanillaSuppressQueue.Add(entity.EntityId);
            }
            catch { }
        }

        public void ProcessSuppressQueue()
        {
            if (_vanillaSuppressQueue.Count == 0) return;
            var done = new List<long>();
            foreach (var id in _vanillaSuppressQueue)
            {
                IMyEntity e;
                if (MyAPIGateway.Entities.TryGetEntityById(id, out e) && e != null)
                {
                    if (_spawned.ContainsKey(id)) { done.Add(id); continue; }
                    e.Close();
                    done.Add(id);
                    Creatures_Logger.Instance?.Debug(SRC, "Vanilla unterdrückt: " + id);
                }
                else done.Add(id);
            }
            foreach (var id in done) _vanillaSuppressQueue.Remove(id);
        }

        private void ScanAndRemoveExistingVanilla()
        {
            var entities = new HashSet<IMyEntity>();
            MyAPIGateway.Entities.GetEntities(entities);
            int count = 0;
            foreach (var e in entities)
            {
                var c = e as IMyCharacter;
                if (c == null) continue;
                string sub = c.Definition?.Id.SubtypeName ?? "";
                if (!VANILLA_SUBTYPES.Contains(sub)) continue;
                if (_spawned.ContainsKey(e.EntityId)) continue;
                e.Close();
                count++;
            }
            Creatures_Logger.Instance?.Info(SRC, "Init-Scan: " + count + " vanilla Kreaturen entfernt");
        }

        // ── Wave-System / Spawn ───────────────────────────────────────────────

        private void TrySpawnAll()
        {
            try
            {
                var players = new List<IMyPlayer>();
                MyAPIGateway.Players.GetPlayers(players);
                if (_rng.Next(12) == 0) RefreshPlanetCache();

                bool isDay = IsDay();
                Creatures_Logger.Instance?.Trace(SRC, "SpawnCheck — Spieler: " + players.Count + " Tag: " + isDay);

                foreach (var player in players)
                {
                    if (player.Character == null || player.Character.IsDead) continue;
                    ulong steamId = player.SteamUserId;

                    // Neuer Spieler — initialen Random-Timer setzen
                    if (!_playerCooldownTicks.ContainsKey(steamId))
                    {
                        int initTicks = _rng.Next(WAVE_TIMER_MIN, WAVE_TIMER_MAX);
                        _playerCooldownTicks[steamId] = initTicks;
                        Creatures_Logger.Instance?.Debug(SRC, "Spieler " + steamId
                            + " → erster Spawn in " + (initTicks / 3600) + "min");
                        continue;
                    }

                    // Cooldown läuft noch
                    if (_playerCooldownTicks[steamId] > 0)
                    {
                        _playerCooldownTicks[steamId] -= SPAWN_INTERVAL_TICKS;
                        continue;
                    }

                    // Noch Kreaturen am Leben → warten bis alle weg sind
                    if (GetTotalPlayerCount(steamId) > 0)
                    {
                        Creatures_Logger.Instance?.Trace(SRC, "Warte — " + GetTotalPlayerCount(steamId) + " Kreaturen aktiv");
                        continue;
                    }

                    // Timer abgelaufen und alle Kreaturen weg → Welle spawnen
                    var playerPos = player.GetPosition();
                    string planetName;
                    var planet = GetNearestPlanet(playerPos, out planetName);
                    if (planet == null) continue;

                    // Höhen-Check
                    var myPlanet = planet as MyPlanet;
                    if (myPlanet != null)
                    {
                        var surface  = myPlanet.GetClosestSurfacePointGlobal(ref playerPos);
                        double alt   = Vector3D.Distance(playerPos, surface);
                        if (alt > MAX_SPAWN_ALTITUDE)
                        {
                            Creatures_Logger.Instance?.Debug(SRC, "Spawn übersprungen — zu hoch: " + alt.ToString("F0") + "m");
                            continue;
                        }
                    }

                    // HEAVY_START vor Spawn-Welle
                    OnHeavyStart?.Invoke("SpawnWelle");
                    bool anySpawned = false;
                    try
                    {
                        foreach (var def in _definitions)
                        {
                            if (!def.Enabled) continue;
                            if (!def.CanSpawnOnPlanet(planetName, isDay)) continue;
                            if (GetPerPlayerCount(def.SubtypeId, steamId) >= def.PerPlayerMax) continue;
                            int countBefore = _spawned.Count;
                            TrySpawnPack(def, player, playerPos, planet, isDay);

                            // Nur als gespawnt zählen wenn wirklich etwas in _spawned gelandet ist
                            if (_spawned.Count > countBefore)
                            {
                                anySpawned = true;
                            }
                        }
                    }
                    finally
                    {
                        OnHeavyEnd?.Invoke("SpawnWelle");
                    }

                    // Kein Spawn möglich → Retry-Cooldown damit HEAVY nicht endlos feuert
                    if (!anySpawned)
                    {
                        _playerCooldownTicks[steamId] = 3600; // 1 Minute Retry
                        Creatures_Logger.Instance?.Debug(SRC,
                            "Kein Spawn für " + steamId + " auf " + planetName +
                            " (Tag: " + isDay + ") — Retry in 1min");
                    }
                }
            }
            catch (Exception ex) { Creatures_Logger.Instance?.Error(SRC, "TrySpawnAll: " + ex.Message); }
        }

        /// <summary>Status-Übersicht für !pbc creatures status</summary>
        public string GetStatus()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("Kreaturen: ").Append(_spawned.Count).Append(" aktiv");
            foreach (var kvp in _playerCooldownTicks)
            {
                int alive = GetTotalPlayerCount(kvp.Key);
                if (alive > 0)
                    sb.Append(" | ").Append(kvp.Key).Append(": ").Append(alive).Append(" aktiv");
                else if (kvp.Value > 0)
                    sb.Append(" | ").Append(kvp.Key).Append(": nächste Welle in ").Append(kvp.Value / 3600).Append("min");
            }
            return sb.ToString();
        }

        /// <summary>Sofort-Spawn für einen Spieler (Command-Interface)</summary>
        public void ForceSpawn(ulong steamId)
        {
            _playerCooldownTicks[steamId] = 0;
            Creatures_Logger.Instance?.Info(SRC, "ForceSpawn für Spieler: " + steamId);
        }

        /// <summary>Sofort-Spawn eines bestimmten Typs (Command-Interface)</summary>
        public void ForceSpawnType(ulong steamId, string subtypeId)
        {
            var players = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(players);
            foreach (var p in players)
            {
                if (p.SteamUserId != steamId) continue;
                var def = GetDefinition(subtypeId);
                if (def == null) return;
                string planetName;
                var planet = GetNearestPlanet(p.GetPosition(), out planetName);
                if (planet == null) return;

                OnHeavyStart?.Invoke("ForceSpawn");
                try
                {
                    TrySpawnPack(def, p, p.GetPosition(), planet, IsDay());
                }
                finally
                {
                    OnHeavyEnd?.Invoke("ForceSpawn");
                }

                Creatures_Logger.Instance?.Info(SRC, "ForceSpawnType " + subtypeId + " für Spieler: " + steamId);
                return;
            }
        }

        // ── Pack-Spawn ────────────────────────────────────────────────────────

        private void TrySpawnPack(CreatureDefinition def, IMyPlayer player, Vector3D playerPos, IMyEntity planet, bool isDay)
        {
            int packMin  = isDay ? def.PackMin : def.NightPackMin;
            int packMax  = isDay ? def.PackMax : def.NightPackMax;
            int packSize = _rng.Next(packMin, packMax + 1);

            Vector3D? groupOrigin = null;
            for (int i = 0; i < packSize; i++)
            {
                if (GetPerPlayerCount(def.SubtypeId, player.SteamUserId) >= def.PerPlayerMax) break;
                if (i == 0)
                    groupOrigin = TrySpawnOne(def, player, playerPos, planet, null);
                else if (groupOrigin.HasValue)
                    TrySpawnOne(def, player, playerPos, planet, groupOrigin);
            }
        }

        private Vector3D? TrySpawnOne(CreatureDefinition def, IMyPlayer player,
            Vector3D playerPos, IMyEntity planet, Vector3D? nearOrigin)
        {
            try
            {
                var myPlanet = planet as MyPlanet;
                if (myPlanet == null) return null;

                for (int attempt = 0; attempt < SPAWN_ATTEMPTS; attempt++)
                {
                    Vector3D coords;

                    if (nearOrigin.HasValue)
                    {
                        var offset = new Vector3D(
                            (_rng.NextDouble() - 0.5) * 100,
                            0,
                            (_rng.NextDouble() - 0.5) * 100);
                        var nearPos = nearOrigin.Value + offset;
                        coords = myPlanet.GetClosestSurfacePointGlobal(ref nearPos);
                    }
                    else
                    {
                        double angle  = _rng.NextDouble() * Math.PI * 2;
                        double radius = def.MinSpawnRadius + _rng.NextDouble() * (def.SpawnRadius - def.MinSpawnRadius);
                        var    flat   = new Vector3D(Math.Cos(angle) * radius, 0, Math.Sin(angle) * radius);
                        var    target = playerPos + flat;
                        coords = myPlanet.GetClosestSurfacePointGlobal(ref target);
                    }

                    if (IsInsideGrid(coords)) continue;
                    if (!nearOrigin.HasValue && IsTooCloseToSameType(coords, def)) continue;

                    var up  = myPlanet.GetClosestSurfacePointGlobal(ref coords) - coords;
                    up.Normalize();
                    var fwd = Vector3D.CalculatePerpendicularVector(up);

                    long entityId = MyVisualScriptLogicProvider.SpawnBot(
                        def.SubtypeId, coords, fwd, up, def.SubtypeId + "_PB");

                    if (entityId == 0)
                    {
                        Creatures_Logger.Instance?.Warn(SRC, "SpawnBot=0: " + def.SubtypeId);
                        continue;
                    }

                    var rule = Creatures_Rules.Get(def.SubtypeId);
                    _spawned[entityId] = new SpawnedCreature
                    {
                        SubtypeId    = def.SubtypeId,
                        OwnerSteamId = player.SteamUserId,
                        SpawnTime    = DateTime.UtcNow,
                        LifetimeSec  = rule.LifetimeSec,
                        SightRadius  = rule.SightRadius
                    };
                    IncrementCount(def.SubtypeId, player.SteamUserId);

                    Creatures_Logger.Instance?.Info(SRC, "Gespawnt: " + def.SubtypeId
                        + " (" + rule.LifetimeSec / 60 + "min)"
                        + " P:" + GetPerPlayerCount(def.SubtypeId, player.SteamUserId) + "/" + def.PerPlayerMax);

                    return coords;
                }

                Creatures_Logger.Instance?.Debug(SRC, "Kein Spawn nach " + SPAWN_ATTEMPTS + " Versuchen: " + def.SubtypeId);
                return null;
            }
            catch (Exception ex) { Creatures_Logger.Instance?.Error(SRC, "TrySpawnOne: " + ex.Message); return null; }
        }

        // ── Despawn ───────────────────────────────────────────────────────────

        private void DespawnCheck()
        {
            try
            {
                var now      = DateTime.UtcNow;
                var toRemove = new List<long>();

                foreach (var kvp in _spawned)
                {
                    var creature = kvp.Value;
                    IMyEntity entity;

                    if (!MyAPIGateway.Entities.TryGetEntityById(kvp.Key, out entity)
                        || entity == null || entity.MarkedForClose)
                    { toRemove.Add(kvp.Key); continue; }

                    double aliveSec = (now - creature.SpawnTime).TotalSeconds;
                    if (aliveSec < creature.LifetimeSec) continue;

                    var pos = entity.GetPosition();

                    if (IsUnderRadiation(pos))
                    {
                        creature.RadiationWaitUntil = now.AddSeconds(60);
                        continue;
                    }
                    if (now < creature.RadiationWaitUntil) continue;

                    toRemove.Add(kvp.Key);
                    Creatures_Logger.Instance?.Info(SRC, "Despawnt: " + creature.SubtypeId
                        + " (lebte " + (int)(aliveSec / 60) + "min)");
                }

                foreach (var id in toRemove) RemoveFromTracking(id);
            }
            catch (Exception ex) { Creatures_Logger.Instance?.Error(SRC, "DespawnCheck: " + ex.Message); }
        }

        // ── Leichen-Check ─────────────────────────────────────────────────────

        private void DeadCheck()
        {
            try
            {
                var now      = DateTime.UtcNow;
                var toRemove = new List<long>();

                foreach (var kvp in _spawned)
                {
                    IMyEntity entity;
                    if (!MyAPIGateway.Entities.TryGetEntityById(kvp.Key, out entity)
                        || entity == null || entity.MarkedForClose)
                    { toRemove.Add(kvp.Key); continue; }

                    var character = entity as IMyCharacter;
                    if (character == null || !character.IsDead) continue;

                    var creature    = kvp.Value;
                    double deadSec  = (now - creature.DeathTime.GetValueOrDefault(now)).TotalSeconds;

                    if (!creature.DeathTime.HasValue)
                    {
                        creature.DeathTime = now;
                        continue;
                    }

                    if (deadSec >= 600)
                    {
                        toRemove.Add(kvp.Key);
                        Creatures_Logger.Instance?.Info(SRC, "Leiche entfernt (10min): " + creature.SubtypeId);
                        continue;
                    }

                    var inventory = character.GetInventory();
                    if (inventory == null || inventory.Empty())
                    {
                        toRemove.Add(kvp.Key);
                        Creatures_Logger.Instance?.Info(SRC, "Leiche entfernt (leer): " + creature.SubtypeId);
                    }
                }

                foreach (var id in toRemove) RemoveFromTracking(id);
            }
            catch (Exception ex) { Creatures_Logger.Instance?.Error(SRC, "DeadCheck: " + ex.Message); }
        }

        private void RemoveFromTracking(long entityId)
        {
            SpawnedCreature c;
            if (!_spawned.TryGetValue(entityId, out c)) return;
            DecrementCount(c.SubtypeId, c.OwnerSteamId);
            _spawned.Remove(entityId);

            if (GetTotalPlayerCount(c.OwnerSteamId) == 0)
            {
                int nextTimer = _rng.Next(WAVE_TIMER_MIN, WAVE_TIMER_MAX);
                _playerCooldownTicks[c.OwnerSteamId] = nextTimer;
                Creatures_Logger.Instance?.Info(SRC, "Alle Kreaturen weg für " + c.OwnerSteamId
                    + " → nächste Welle in " + (nextTimer / 3600) + "min");
            }
        }

        // ── Radiation ─────────────────────────────────────────────────────────

        private void RegisterDamageHandler()
        {
            try
            {
                // FIX: ref-Parameter in Lambda nicht erlaubt in C# 6 → named method
                MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(0, DamageHandler);
                Creatures_Logger.Instance?.Info(SRC, "Radiation-Schutz aktiv");
            }
            catch (Exception ex) { Creatures_Logger.Instance?.Warn(SRC, "DamageHandler: " + ex.Message); }
        }

        private void DamageHandler(object target, ref MyDamageInformation info)
        {
            try
            {
                var character = target as IMyCharacter;
                if (character == null) return;
                if (!_spawned.ContainsKey(character.EntityId)) return;
                if (info.Type == MyStringHash.GetOrCompute("Radioactivity")
                ||  info.Type == MyStringHash.GetOrCompute("Environment"))
                    info.Amount = 0f;
            }
            catch { }
        }

        private void ScanRadiationWeather()
        {
            try
            {
                _radiationWeather.Clear();
                var defs = MyDefinitionManager.Static.GetDefinitionsOfType<MyWeatherEffectDefinition>();
                foreach (var d in defs)
                {
                    if (d == null) continue;
                    if (d.RadiationHazard != null && d.RadiationHazard.RadiationGain < 0)
                    {
                        _radiationWeather.Add(d.Id.SubtypeName);
                        Creatures_Logger.Instance?.Trace(SRC, "Radiation-Wetter: " + d.Id.SubtypeName);
                    }
                }
                Creatures_Logger.Instance?.Debug(SRC, "Radiation-Wetter gesamt: " + _radiationWeather.Count);
            }
            catch
            {
                _radiationWeather.Add("AlienRainLight");    _radiationWeather.Add("AlienRainHeavy");
                _radiationWeather.Add("AlienThunderstormLight"); _radiationWeather.Add("AlienThunderstormHeavy");
                _radiationWeather.Add("RainLight");         _radiationWeather.Add("RainHeavy");
                _radiationWeather.Add("ThunderstormLight"); _radiationWeather.Add("ThunderstormHeavy");
                Creatures_Logger.Instance?.Warn(SRC, "Radiation-Fallback aktiv: " + _radiationWeather.Count);
            }
        }

        private bool IsUnderRadiation(Vector3D pos)
        {
            try
            {
                var w = MyAPIGateway.Session.WeatherEffects;
                if (w == null) return false;
                string id = w.GetWeather(pos) ?? "";
                return !string.IsNullOrEmpty(id) && _radiationWeather.Contains(id);
            }
            catch { return false; }
        }

        // ── Hilfsmethoden ─────────────────────────────────────────────────────

        private void RefreshPlanetCache()
        {
            try
            {
                _planetCache.Clear();
                var entities = new HashSet<IMyEntity>();
                MyAPIGateway.Entities.GetEntities(entities);
                foreach (var e in entities)
                {
                    var voxel = e as IMyVoxelBase;
                    if (voxel == null || string.IsNullOrEmpty(voxel.StorageName)) continue;
                    if (e.WorldVolume.Radius < 5000) continue;
                    string name = ExtractPlanetName(voxel.StorageName);
                    _planetCache[name] = e;
                    Creatures_Logger.Instance?.Trace(SRC, "Planet: " + voxel.StorageName + " -> " + name);
                }
                Creatures_Logger.Instance?.Debug(SRC, "Planeten gefunden: " + _planetCache.Count);
            }
            catch (Exception ex) { Creatures_Logger.Instance?.Error(SRC, "RefreshPlanetCache: " + ex.Message); }
        }

        private string ExtractPlanetName(string storageName)
        {
            int dash = storageName.IndexOf('-');
            return dash > 0 ? storageName.Substring(0, dash) : storageName;
        }

        private IMyEntity GetNearestPlanet(Vector3D pos, out string planetName)
        {
            planetName = null;
            IMyEntity nearest = null;
            double minDist = double.MaxValue;
            foreach (var kvp in _planetCache)
            {
                double dist = Vector3D.Distance(pos, kvp.Value.GetPosition());
                if (dist < minDist) { minDist = dist; nearest = kvp.Value; planetName = kvp.Key; }
            }
            return nearest;
        }

        private bool IsDay()
        {
            try
            {
                double rot = MyAPIGateway.Session.SessionSettings.SunRotationIntervalMinutes;
                if (rot <= 0) return true;
                double mins = (DateTime.UtcNow.TimeOfDay.TotalMinutes % rot);
                return (mins / rot) < 0.5;
            }
            catch { return true; }
        }

        private bool IsInsideGrid(Vector3D pos)
        {
            try
            {
                var entities = new HashSet<IMyEntity>();
                MyAPIGateway.Entities.GetEntities(entities);
                foreach (var e in entities)
                {
                    var grid = e as IMyCubeGrid;
                    if (grid != null && grid.WorldAABB.Contains(pos) != ContainmentType.Disjoint)
                        return true;
                }
                return false;
            }
            catch { return false; }
        }

        private bool IsTooCloseToSameType(Vector3D pos, CreatureDefinition def)
        {
            foreach (var kvp in _spawned)
            {
                if (kvp.Value.SubtypeId != def.SubtypeId) continue;
                IMyEntity e;
                if (MyAPIGateway.Entities.TryGetEntityById(kvp.Key, out e) && e != null)
                    if (Vector3D.Distance(pos, e.GetPosition()) < def.MinCreatureSpacing)
                        return true;
            }
            return false;
        }

        // ── Zähler ────────────────────────────────────────────────────────────

        private void IncrementCount(string subtype, ulong steamId)
        {
            if (!_perPlayerCount.ContainsKey(subtype)) _perPlayerCount[subtype] = new Dictionary<ulong, int>();
            if (!_perPlayerCount[subtype].ContainsKey(steamId)) _perPlayerCount[subtype][steamId] = 0;
            _perPlayerCount[subtype][steamId]++;
        }

        private void DecrementCount(string subtype, ulong steamId)
        {
            Dictionary<ulong, int> map;
            if (_perPlayerCount.TryGetValue(subtype, out map) && map.ContainsKey(steamId))
                map[steamId] = Math.Max(0, map[steamId] - 1);
        }

        private int GetPerPlayerCount(string subtype, ulong steamId)
        {
            Dictionary<ulong, int> map;
            if (!_perPlayerCount.TryGetValue(subtype, out map)) return 0;
            int c; map.TryGetValue(steamId, out c); return c;
        }

        private int GetTotalPlayerCount(ulong steamId)
        {
            int total = 0;
            foreach (var kvp in _spawned)
                if (kvp.Value.OwnerSteamId == steamId) total++;
            return total;
        }

        private CreatureDefinition GetDefinition(string subtype)
        {
            foreach (var def in _definitions)
                if (string.Equals(def.SubtypeId, subtype, StringComparison.OrdinalIgnoreCase)) return def;
            return null;
        }

        // ── SpawnedCreature ───────────────────────────────────────────────────

        private class SpawnedCreature
        {
            public string    SubtypeId;
            public ulong     OwnerSteamId;
            public DateTime  SpawnTime;
            public int       LifetimeSec;
            public double    SightRadius;
            public DateTime? DeathTime          = null;
            public DateTime  RadiationWaitUntil = DateTime.MinValue;
        }
    }
}