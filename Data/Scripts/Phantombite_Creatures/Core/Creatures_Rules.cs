using System.Collections.Generic;

namespace PhantombiteCreatures.Core
{
    /// <summary>
    /// Hardcodierte Regeln und Verhaltensmuster pro Kreatur-Typ.
    ///
    /// Kategorie bestimmt grundlegende Spawn-Logik.
    /// BehaviorPattern bestimmt KI-Verhalten (aktiv sobald AiEnabled geladen ist).
    ///
    /// ── Kategorien ──────────────────────────────────────────────────────────
    /// Underground  — Erscheinen aus dem Boden, unsichtbar auf Distanz
    /// Surface      — Laufen oben, von weitem sichtbar
    /// Apex         — Seltenste, stärkste Kreatur
    ///
    /// ── Verhaltensmuster (AiEnabled) ────────────────────────────────────────
    /// Passive       — Läuft nur herum, greift nie an
    /// AggroOnClose  — Passiv bis Spieler < AggroRadius, dann angriff
    /// PackHunter    — Greift nur an wenn Gruppe >= PackSize vorhanden
    /// AlwaysAggro   — Greift sofort alles an, kein Flüchten (Apex)
    /// Ambush        — Passiv bis Spieler < AmbushRadius, dann sofortangriff
    /// FleeWhenHurt  — Greift an, flieht aber wenn HP unter FleeHealthPct
    /// </summary>
    public static class Creatures_Rules
    {
        // ── Kategorien ────────────────────────────────────────────────────────

        public enum CreatureCategory { Underground, Surface, Apex }

        // ── Verhaltensmuster ──────────────────────────────────────────────────

        public enum BehaviorPattern
        {
            Passive,        // Läuft rum, greift nie an
            AggroOnClose,   // Passiv bis AggroRadius → dann Angriff
            PackHunter,     // Angriff erst wenn Gruppe >= PackSize
            AlwaysAggro,    // Greift sofort alles an (Apex-Typ)
            Ambush,         // Unterirdisch passiv, Angriff bei AmbushRadius
            FleeWhenHurt    // Kämpft bis HP < FleeHealthPct, dann flieht
        }

        // ── Kreatur-Regel ─────────────────────────────────────────────────────

        public class CreatureRule
        {
            public CreatureCategory Category;
            public BehaviorPattern  Behavior;

            /// <summary>AiEnabled Rollen-String für SpawnBotQueued</summary>
            public string AiEnabledRole;

            /// <summary>Distanz in Metern ab der AggroOnClose greift</summary>
            public double AggroRadius     = 200;

            /// <summary>Mindestgröße der Gruppe für PackHunter</summary>
            public int    PackSize        = 3;

            /// <summary>Distanz für Ambush-Trigger</summary>
            public double AmbushRadius    = 80;

            /// <summary>HP-Prozent unter dem FleeWhenHurt flieht (0-1)</summary>
            public float  FleeHealthPct   = 0.3f;

            /// <summary>Lebenszeit in Sekunden — Kreatur despawnt frühestens nach dieser Zeit</summary>
            public int    LifetimeSec     = 900; // 15 Minuten

            /// <summary>Sichtweite des Spielers — kein Despawn wenn Spieler näher</summary>
            public double SightRadius     = 500;

            public CreatureRule(
                CreatureCategory category,
                BehaviorPattern  behavior,
                string           aiRole        = "CREATURE",
                double           aggroRadius   = 200,
                int              packSize      = 3,
                double           ambushRadius  = 80,
                float            fleeHealthPct = 0.3f,
                int              lifetimeSec   = 900,
                double           sightRadius   = 500)
            {
                Category      = category;
                Behavior      = behavior;
                AiEnabledRole = aiRole;
                AggroRadius   = aggroRadius;
                PackSize      = packSize;
                AmbushRadius  = ambushRadius;
                FleeHealthPct = fleeHealthPct;
                LifetimeSec   = lifetimeSec;
                SightRadius   = sightRadius;
            }
        }

        // ── Regeln pro SubtypeId ──────────────────────────────────────────────

        private static readonly Dictionary<string, CreatureRule> Rules
            = new Dictionary<string, CreatureRule>(System.StringComparer.OrdinalIgnoreCase)
        {
            // ── Spinnen — Underground, Ambush ────────────────────────────────
            // Passiv bis Spieler sehr nah, dann Sofortangriff aus dem Boden
            {
                "SpaceSpider", new CreatureRule(
                    category:    CreatureCategory.Underground,
                    behavior:    BehaviorPattern.Ambush,
                    aiRole:      "CREATURE",
                    ambushRadius: 80,
                    lifetimeSec:  900,
                    sightRadius:  200
                )
            },
            {
                "SpaceSpiderBrown", new CreatureRule(
                    category:    CreatureCategory.Underground,
                    behavior:    BehaviorPattern.Ambush,
                    aiRole:      "CREATURE",
                    ambushRadius: 80,
                    lifetimeSec:  900,
                    sightRadius:  200
                )
            },
            {
                "SpaceSpiderBlack", new CreatureRule(
                    category:    CreatureCategory.Underground,
                    behavior:    BehaviorPattern.Ambush,
                    aiRole:      "CREATURE",
                    ambushRadius: 80,
                    lifetimeSec:  900,
                    sightRadius:  200
                )
            },

            // ── Wölfe — Surface, AggroOnClose ────────────────────────────────
            // Laufen sichtbar rum, greifen erst bei Nähe an
            {
                "Wolf", new CreatureRule(
                    category:    CreatureCategory.Surface,
                    behavior:    BehaviorPattern.AggroOnClose,
                    aiRole:      "CREATURE",
                    aggroRadius: 300,
                    lifetimeSec:  900,
                    sightRadius:  800
                )
            },
            {
                "WolfHowling", new CreatureRule(
                    category:    CreatureCategory.Surface,
                    behavior:    BehaviorPattern.AggroOnClose,
                    aiRole:      "CREATURE",
                    aggroRadius: 300,
                    lifetimeSec:  900,
                    sightRadius:  800
                )
            },

            // ── Custom Kreaturen ─────────────────────────────────────────────
            // Werden hier eingetragen sobald SubtypeIds feststehen

            // Raptor — Surface, aggressiv bei mittlerer Distanz
            // { "PB_Raptor", new CreatureRule(
            //     category:    CreatureCategory.Surface,
            //     behavior:    BehaviorPattern.AggroOnClose,
            //     aiRole:      "CREATURE",
            //     aggroRadius: 400
            // )},

            // Frosch — Surface, Rudeljäger
            // { "PB_Frog", new CreatureRule(
            //     category:    CreatureCategory.Surface,
            //     behavior:    BehaviorPattern.PackHunter,
            //     aiRole:      "CREATURE",
            //     packSize:    3,
            //     aggroRadius: 300
            // )},

            // Shark — Apex, greift sofort alles an
            // { "PB_Shark", new CreatureRule(
            //     category:    CreatureCategory.Apex,
            //     behavior:    BehaviorPattern.AlwaysAggro,
            //     aiRole:      "CREATURE",
            //     aggroRadius: 1000
            // )},

            // Sandwurm — Underground, Ambush mit großem Radius
            // { "PB_Sandworm", new CreatureRule(
            //     category:    CreatureCategory.Underground,
            //     behavior:    BehaviorPattern.Ambush,
            //     aiRole:      "CREATURE",
            //     ambushRadius: 150
            // )},
        };

        // ── Default ───────────────────────────────────────────────────────────

        private static readonly CreatureRule DEFAULT = new CreatureRule(
            category: CreatureCategory.Surface,
            behavior: BehaviorPattern.AggroOnClose
        );

        public static CreatureRule Get(string subtypeId)
        {
            CreatureRule rule;
            return Rules.TryGetValue(subtypeId, out rule) ? rule : DEFAULT;
        }

        public static CreatureCategory GetCategory(string subtypeId)
            => Get(subtypeId).Category;

        public static BehaviorPattern GetBehavior(string subtypeId)
            => Get(subtypeId).Behavior;
    }
}