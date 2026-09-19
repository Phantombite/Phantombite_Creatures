# DEV Funktion — Phantombite Creatures

Stand: 2026-09-19 · Version 1.0.0 · Workshop-ID 3728225683 · Core-Kanal 1995003 · Kurzname `creatures`

## Zweck
Lässt Kreaturen (Wolf, Weltraum-Spinnen) in der Nähe von Spielern spawnen, mit eigener Regel-Logik pro Planet
und Tageszeit. Nutzt **kein MES mehr**: das Spawn-System ist eigener Code (siehe auch `Phantombite_Encounter_System`).

## Kreaturen und Regeln (Standard-Config)
| Definition | Planet / Zeit |
|---|---|
| `Wolf` | EarthLike, Tag 1–2, Nacht 2–4 |
| `SpaceSpider`, `SpaceSpiderBrown`, `SpaceSpiderBlack` | Alien und Pertam, Tag 1–4 |

## Konfiguration
`PhantombiteCreatures_Config.ini` im World-Storage (nur Server, wird bei Fehlen mit den Standardwerten angelegt).
Pro Definition ein Abschnitt (`[Wolf]`, `[SpaceSpider]`, ...).

## Commands (`!pbc creatures ...`)
| Command | Admin | Wirkung |
|---|---|---|
| `status` | nein | Spawn-Timer aller Spieler anzeigen |
| `spawn [wolf\|spider\|spiderbrown\|spiderblack] [Spielername]` | ja | Kreatur spawnen (ohne Typ zufällig, ohne Spielername bei dir) |

## Ablauf
- Alle 5 s: Spawn-Prüfung pro Spieler (mit Abklingzeit), alle 10 s: Despawn-Prüfung, alle 10 s: Leichen-Prüfung.
- Spawn-Position über die Planetenoberfläche (`GetClosestSurfacePointGlobal`).
- Beim Start wartet der Mod auf das `READY` des Core, nach 10 s startet er notfalls allein (Fallback-Init).

## Core-Anbindung
Empfängt `READY`, `LOGLEVEL`, `PERFLEVEL`, `CMD`, meldet `HEAVY_START`/`HEAVY_END` um Spawn-Wellen.
**Performance-Level:** 1–2 halbiert die Update-Rate, 3 schaltet den Mod aus (kein Update).

## Dateien
`Core/Creatures_Session.cs` (Einstieg und Spawn-Logik, nur Server), `Modules/Creature_Command.cs` (Anbindung an den Core:
READY, REGISTER, CMD, PERFLEVEL), `Creatures_SpawnManager.cs` (Spawn, Despawn, Suppress-Queue), `Creatures_Rules.cs` (Regeln),
`Modules/Creatures_Definition.cs`, `Creatures_FileManager.cs`, `Creatures_Logger.cs`.

## Offene Punkte / Roadmap
- [ ] **Totalabsturz durch Radioaktivität** (aus der Roadmap): Ursache noch offen
- [ ] Angleichen an `0_Phantombite_MOD_TEMPLATE.md` (README, patch_notes, thumb.jpg)
- [ ] Alle 5–10 s werden alle Entities der Welt durchsucht (Spawn-, Despawn- und Leichen-Prüfung), bei großen Welten prüfen
- [ ] Kommentar im Code zu Performance-Level 2 („kein automatischer Spawn“) stimmt nicht mit dem Verhalten überein (halbe Rate)
