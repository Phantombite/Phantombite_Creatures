using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.ModAPI;
using PhantombiteCreatures.Modules;

namespace PhantombiteCreatures.Modules
{
    public static class Creatures_FileManager
    {
        private const string SRC      = "Creatures_FileManager";
        private const string FILENAME = "PhantombiteCreatures_Config.ini";

        // ── Vanilla-Template ──────────────────────────────────────────────────
        // Direkt aus PlanetGeneratorDefinitions.sbc abgeleitet.
        // EarthLike: Wolf (Tag 1-2, Nacht 2-4)
        // Alien+Pertam: SpaceSpider-Arten (Tag 1-4)
        private static readonly string DEFAULT_CONFIG = @"; Phantombite Creatures Config
; Alle 8 Vanilla-Planeten: EarthLike, Alien, Pertam, Mars, Titan, Moon, Europa, Triton
; Leer lassen = kein Spawn auf diesem Planeten zu dieser Zeit

[Wolf]
Enabled=true
DayPlanets=EarthLike
NightPlanets=EarthLike,Moon
PerPlayerMax=5
MinSpawnRadius=500
SpawnRadius=1000
MinCreatureSpacing=300
PackMin=1
PackMax=2
NightPackMin=2
NightPackMax=4

[SpaceSpider]
Enabled=true
DayPlanets=Alien,Pertam,Mars
NightPlanets=Alien,Pertam,Mars,Europa,Triton
PerPlayerMax=4
MinSpawnRadius=200
SpawnRadius=500
MinCreatureSpacing=300
PackMin=1
PackMax=2
NightPackMin=1
NightPackMax=4

[SpaceSpiderBrown]
Enabled=true
DayPlanets=Alien,Pertam,Titan
NightPlanets=Alien,Pertam,Titan,Mars
PerPlayerMax=4
MinSpawnRadius=200
SpawnRadius=500
MinCreatureSpacing=300
PackMin=1
PackMax=2
NightPackMin=1
NightPackMax=4

[SpaceSpiderBlack]
Enabled=true
DayPlanets=Alien,Pertam
NightPlanets=Alien,Pertam,Triton,Europa,Moon
PerPlayerMax=3
MinSpawnRadius=200
SpawnRadius=500
MinCreatureSpacing=300
PackMin=1
PackMax=2
NightPackMin=1
NightPackMax=3
";

        public static List<CreatureDefinition> Load()
        {
            try
            {
                if (!MyAPIGateway.Utilities.FileExistsInWorldStorage(FILENAME, typeof(Creatures_FileManager)))
                {
                    MyAPIGateway.Utilities.WriteFileInWorldStorage(FILENAME, typeof(Creatures_FileManager))
                        .Write(DEFAULT_CONFIG);
                    Creatures_Logger.Instance?.Info(SRC, "Config erstellt: " + FILENAME);
                }

                string raw;
                using (var reader = MyAPIGateway.Utilities.ReadFileInWorldStorage(FILENAME, typeof(Creatures_FileManager)))
                    raw = reader.ReadToEnd();

                var defs = Parse(raw);
                Creatures_Logger.Instance?.Info(SRC, "Config geladen: " + FILENAME);
                Creatures_Logger.Instance?.Info(SRC, defs.Count + " Kreatur-Definition(en) geladen");
                return defs;
            }
            catch (Exception ex)
            {
                Creatures_Logger.Instance?.Error(SRC, "Load: " + ex.Message);
                return new List<CreatureDefinition>();
            }
        }

        private static List<CreatureDefinition> Parse(string raw)
        {
            var defs    = new List<CreatureDefinition>();
            CreatureDefinition current = null;

            foreach (var rawLine in raw.Split('\n'))
            {
                string line = rawLine.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith(";")) continue;

                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    current = new CreatureDefinition { SubtypeId = line.Substring(1, line.Length - 2) };
                    defs.Add(current);
                    continue;
                }

                if (current == null) continue;

                int eq = line.IndexOf('=');
                if (eq < 0) continue;
                string key = line.Substring(0, eq).Trim();
                string val = line.Substring(eq + 1).Trim();

                switch (key)
                {
                    case "Enabled":          current.Enabled    = val.Equals("true", StringComparison.OrdinalIgnoreCase); break;
                    case "DayPlanets":       SetPlanets(current.DayPlanets, val);   break;
                    case "NightPlanets":     SetPlanets(current.NightPlanets, val); break;
                    case "PerPlayerMax":     int.TryParse(val, out current.PerPlayerMax);       break;
                    case "MinSpawnRadius":   double.TryParse(val, out current.MinSpawnRadius);  break;
                    case "SpawnRadius":      double.TryParse(val, out current.SpawnRadius);     break;
                    case "MinCreatureSpacing": double.TryParse(val, out current.MinCreatureSpacing); break;
                    case "PackMin":          int.TryParse(val, out current.PackMin);            break;
                    case "PackMax":          int.TryParse(val, out current.PackMax);            break;
                    case "NightPackMin":     int.TryParse(val, out current.NightPackMin);       break;
                    case "NightPackMax":     int.TryParse(val, out current.NightPackMax);       break;
                }
            }
            return defs;
        }

        private static void SetPlanets(HashSet<string> set, string val)
        {
            set.Clear();
            if (string.IsNullOrWhiteSpace(val)) return;
            foreach (var p in val.Split(','))
            {
                string trimmed = p.Trim();
                if (!string.IsNullOrEmpty(trimmed)) set.Add(trimmed);
            }
        }
    }
}