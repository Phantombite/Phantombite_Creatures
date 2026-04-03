# DEV TODO — Phantombite Creatures

## Offen

- [ ] HugeWorm HP anpassen — aktuell 50, zu wenig für Apex Unterirdisch.
- [ ] HugeWorm Spawn aktivieren — aktuell auskommentiert in SpawnDefinition_Worm.sbc.
- [ ] PlanetWhitelist anpassen sobald Wüstenplanet umbenannt wird (aktuell: Dune).
- [ ] Fusion mit Planetenmod — PlanetGeneratorDefinitions einbauen.
- [ ] Factions_Creature.sbc SANI aufräumen — wird nicht genutzt, alle Bots nutzen SPID.
- [ ] TotalBotLimit in Welteinstellungen auf 80-100 setzen — für OrganicPlasma Schwärme.
- [ ] MES Config setzen: `/MES.Settings.Creatures.OverrideVanillaCreatureSpawns.true`.
- [ ] Server-Test ausstehend — Tag/Nacht Spawn, Loot, Schwarm-Verhalten.
- [ ] Raptor und Groump haben kein Idle Animation — Walk als Fallback gesetzt.
- [ ] Worm Stats DefaultValue prüfen — aktuell 50 HP, Worm Mod Original hatte keine DefaultValue.

## Bekannte Probleme
- Raptor greift nicht immer zuverlässig an — BehaviorType Wolf, Tetrapod_simple Skelett.
- HugeWorm Spawn deaktiviert bis HP und Spawn-Werte final abgestimmt.

## Erledigt
- [x] Alien Kreaturen umbenannt: Shark, Groump, Raptor, OrganicPlasma.
- [x] MOD_CRITICAL_ERROR behoben — `<n>` Tags zu `<Name>` korrigiert.
- [x] Loot System auf Bots.sbc umgestellt.
- [x] Vanilla Fleisch eingebaut (MammalMeatRaw, InsectMeatRaw).
- [x] Tag/Nacht Spawn konfiguriert.
- [x] AttackLength auf 700 vereinheitlicht.
- [x] Worm Mod integriert und Pfade angepasst.
- [x] Mod zu Phantombite_Creatures fusioniert.
- [x] OrganicPlasma Schwarm 10-20 konfiguriert.
- [x] DefaultValue in Stats_Alien.sbc hinzugefügt.
