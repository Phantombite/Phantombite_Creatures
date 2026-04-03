# DEV Funktion — Phantombite Creatures

## Zweck
Fügt dem Wüstenplaneten Dune ein vollständiges Ökosystem aus 6 Kreaturen hinzu.
Unterirdische Kreaturen kommen aus dem Boden. Oberirdische sind nachtaktiv.
MES übernimmt den kompletten Spawn — kein vanilla Creature Spawn.

## Kreaturen Übersicht

### Oberirdisch — Nur nachts aktiv

| Kreatur | SubtypeId | HP | Schaden | Sprint | Gruppe | Loot |
|---------|-----------|-----|---------|--------|--------|------|
| Shark | Shark | 400 | 40 | 10 | 1-2 | 16-24 MammalMeatRaw |
| Groump | Groump | 200 | 20 | 10 | 3-8 | 4-6 MammalMeatRaw |
| Raptor | Raptor | 250 | 25 | 12 | 2-4 | 1-3 MammalMeatRaw |

### Unterirdisch — Tag und Nacht aktiv

| Kreatur | SubtypeId | HP | Schaden | Sprint | Gruppe | Loot |
|---------|-----------|-----|---------|--------|--------|------|
| OrganicPlasma | OrganicPlasma | 50 | 12 | 15 | 10-20 | 1-2 InsectMeatRaw |
| Worm | Space_spider_worm | 50 | 15 | 11 | 2-15 | SpiderLoot |
| HugeWorm | Space_spider_hugeworm | 50 | 15 | ? | 1 | SpiderLoot |

## Spawn Einstellungen

| Kreatur | Frequency | PlanetWhitelist | Tag/Nacht |
|---------|-----------|-----------------|-----------|
| Shark | 3.0 | Dune | Nur nachts |
| Groump | 8.0 | Dune | Nur nachts |
| Raptor | 6.0 | Dune | Nur nachts |
| OrganicPlasma | 10.0 | Dune | Immer |
| Worm | 5.0 | Pertam, DustyDesertPlanet, Vector, Satreus, Dune | Immer |
| HugeWorm | — | — | Auskommentiert |

## Dateistruktur
```
Phantombite_Creatures/
├── Audio/
│   ├── ArcBeast.wav, ArcPoof.wav, ArcSquelch.wav
│   ├── Beast01-03.wav, BeastWalk01-02.wav
│   └── Worm_Attack.wav, Worm_Death.wav, Worm_Hit.wav
├── Data/
│   ├── Animations.sbc
│   ├── Audio_Alien.sbc / Audio_Worm.sbc
│   ├── Bots_Alien.sbc / Bots_Worm.sbc
│   ├── Characters_Shark.sbc
│   ├── Characters_Groump.sbc
│   ├── Characters_Raptor_OrganicPlasma.sbc
│   ├── Characters_Worm.sbc / Characters_HugeWorm.sbc
│   ├── ContainerTypes.sbc
│   ├── EntityComponents_Alien.sbc / EntityComponents_Worm.sbc
│   ├── EntityContainers_Alien.sbc / EntityContainers_Worm.sbc
│   ├── Factions_Creature.sbc
│   ├── MaterialProperties_Alien.sbc / MaterialProperties_Worm.sbc
│   ├── PhysicalMaterials_Alien.sbc / PhysicalMaterials_Worm.sbc
│   ├── SpawnDefinition_Alien.sbc / SpawnDefinition_Worm.sbc
│   ├── Stats_Alien.sbc / Stats_Worm.sbc
│   ├── AnimationControllers/
│   │   ├── AC_Shark.sbc, AC_Groump.sbc, AC_Raptor.sbc, AC_OrganicPlasma.sbc
│   │   ├── AC_Worm.sbc, AC_HugeWorm.sbc
│   └── Prefab/MES-CreaturePrefabDummy.sbc
├── Models/Characters/
│   ├── Shark.mwm, Groump.mwm, Raptor.mwm, OrganicPlasma.mwm
│   ├── Worm.mwm, HugeWorm.mwm
│   └── Animations/ (26 Animationsdateien)
└── Textures/Models/Characters/
    ├── Shark/Groump/Raptor/OrganicPlasma _cm/_ng/_add
    └── SandWorm_cm/_ng, Acid_cm/_ng
```

## Technische Hinweise
- `<Name>` Tags in Characters SBC müssen großgeschrieben sein — `<n>` crasht den Mod.
- Loot wird über `InventoryContainerTypeId` in Bots.sbc gesteuert — nicht in Characters.
- `SpiderBehavior` nur mit Sabiroid Skelett verwendbar — Tetrapod_simple nutzt WolfBehavior.
- AttackLength steuert Erkennungsreichweite — alle Kreaturen auf 700 gesetzt.
- Tag/Nacht via MES Tags: `[UseDayOrNightOnly:true]` + `[SpawnOnlyAtNight:true]`.
- TotalBotLimit in Welteinstellungen auf 80-100 erhöhen für OrganicPlasma Schwärme.
