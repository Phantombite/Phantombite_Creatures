# DEV History — Phantombite Creatures

## 2026-09-19 — Neuere Fassung aus dem Mods-Ordner übernommen
Im Mods-Ordner lag ein späterer, nie committeter Stand (18./19. Mai): Die Core-Anbindung ist in eine eigene Datei
(`Creature_Command.cs`) ausgelagert, dazu Pandora als Spawn-Planet, höheres Höhenlimit (5000 m statt 150 m) und eine
Statusanzeige mit Spielernamen. Dieser Stand ersetzt die kompakte Fassung von GitHub (Session mit allem in einer Datei).
- Die Commands sind jetzt `spawn [typ] [spieler]` und `status`. `timer` gibt es nicht mehr.
- Korrigiert: `HEAVY_START/END` wurden als `creature` gemeldet, der Mod registriert sich aber als `creatures`
  (der Core konnte Last nicht zuordnen); Hilfetext zeigte `!pbc creature`.
- Kompiliert fehlerfrei. Die Änderungen weiter unten (Admin-Flag, `using`) betrafen die abgelöste Fassung.

## 2026-09-19 — Bereinigung
- **Sicherheitsfehler behoben:** Die Commands wurden mit `true`/`false` statt `1`/`0` registriert. Der Core wertete
  alles außer `1` als „für alle Spieler“, damit konnte jeder `spawn` und `timer` ausführen. Jetzt `status:0`,
  `spawn:1`, `timer:1`. Der Core behandelt `true` inzwischen ebenfalls als „nur Admin“.
- Compile-Fehler behoben: fehlendes `using VRage.Game.ModAPI;` (`IMyPlayer`)
- Ungenutztes Feld `_coreReady` entfernt
- Doku angelegt, `.gitignore` ergänzt
