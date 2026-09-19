# DEV History — Phantombite Creatures

## 2026-09-19 — Bereinigung
- **Sicherheitsfehler behoben:** Die Commands wurden mit `true`/`false` statt `1`/`0` registriert. Der Core wertete
  alles außer `1` als „für alle Spieler“, damit konnte jeder `spawn` und `timer` ausführen. Jetzt `status:0`,
  `spawn:1`, `timer:1`. Der Core behandelt `true` inzwischen ebenfalls als „nur Admin“.
- Compile-Fehler behoben: fehlendes `using VRage.Game.ModAPI;` (`IMyPlayer`)
- Ungenutztes Feld `_coreReady` entfernt
- Doku angelegt, `.gitignore` ergänzt
