# DEV History — Phantombite Creatures

## 2026-03-23 — v1.0.0 — Initialer Release (in Entwicklung)

### Ursprung
- Alien Kreaturen aus einem bestehenden Mod extrahiert und vollständig überarbeitet.
- Worm Mod integriert und in einheitliche Struktur gebracht.
- Beide Mods zu Phantombite_Creatures fusioniert.

### Alien Kreaturen
- Alle SubtypeIds umbenannt: LavaPup→Groump, Alien_Beast→Shark, ExplodingCreature→OrganicPlasma, Creature3→Raptor.
- Alle Dateinamen entsprechend angepasst.
- `<n>` Tags zu `<Name>` korrigiert — behoben MOD_CRITICAL_ERROR beim Laden.
- Loot System von Characters auf Bots.sbc verschoben (InventoryContainerTypeId).
- Vanilla Fleisch eingebaut: MammalMeatRaw für Säugetiere, InsectMeatRaw für OrganicPlasma.
- Loot Mengen skaliert nach Körpergröße (Wolf = Referenz 2-3 Fleisch).
- HP, Schaden, Sprint auf Rangordnung abgestimmt: Shark > Groump > Raptor > OrganicPlasma.
- AttackLength für alle auf 700 gesetzt (Worm Referenzwert).
- Tag/Nacht Spawn eingebaut: Shark/Groump/Raptor nur nachts, OrganicPlasma immer.
- OrganicPlasma Schwarm: 10-20 Spawn, Frequency 10.0.
- DefaultValue in Stats hinzugefügt (fehlte in Original).
- AssetModifierComponent und CharacterDiscoveryComponent entfernt (für Spielercharaktere).
- Bags/Carcass/Tentacle Item-System vollständig entfernt.
- AC_Creature3.sbc (alt, unbenutzt) entfernt.
- Factions_Creature.sbc (SANI) vorhanden aber ungenutzt — alle Bots nutzen SPID.

### Worm Mod Integration
- Worm Mod Pfade angepasst: Models\Animation\ → Models\Characters\Animations\.
- WormSpider.mwm → Worm.mwm, Huge_WormSpider.mwm → HugeWorm.mwm.
- Huge_Worm_*.mwm → HugeWorm_*.mwm.
- Content\ Ordner (doppelte Animationen) entfernt.
- PlanetWhitelist Dune zu Worm SpawnDefinition hinzugefügt.

### Offene Punkte bei Release
- HugeWorm HP noch auf 50 — Anpassung ausstehend.
- PlanetWhitelist noch auf Dune — bei Planetenumbenennung anpassen.
- Factions_Creature.sbc SANI wird nicht genutzt — kann gelöscht werden.
- Fusion mit Planetenmod ausstehend (PlanetGeneratorDefinitions).
