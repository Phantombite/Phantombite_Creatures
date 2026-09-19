# Phantombite Creatures

Lässt **Kreaturen** in der Nähe von Spielern spawnen: Wölfe und Weltraum-Spinnen, mit eigenen Regeln pro Planet und Tageszeit.
Kein MES nötig, das Spawn-System ist eigener Code.

## Funktionen
| Kreatur | Wo / Wann (Standard) |
|---|---|
| Wolf | EarthLike, Tag 1–2, Nacht 2–4 (weitere Planeten in der Config einstellbar) |
| SpaceSpider, SpaceSpiderBrown, SpaceSpiderBlack | Alien, Pertam und weitere (u. a. Pandora), Tag 1–4 |

- Spawn in der Nähe von Spielern mit Abklingzeit, automatisches Aufräumen zu weit entfernter Kreaturen und von Leichen
- Alles in einer Config einstellbar
- Reagiert auf die Server-Last: bei Überlast halbe Rate, bei sehr hoher Last aus (über den Phantombite Core)

## Commands
```
!pbc creatures <command>
```
| Command | Wer | Beschreibung |
|---|---|---|
| `status` | alle | Spawn-Timer aller Spieler anzeigen |
| `spawn [wolf\|spider\|spiderbrown\|spiderblack] [Spielername]` | Admin | Kreatur spawnen: ohne Typ zufällig, ohne Spielername bei dir selbst |

## Konfiguration
`PhantombiteCreatures_Config.ini` im World-Storage (wird beim ersten Start mit Standardwerten angelegt, ein Abschnitt pro Kreatur).

## Voraussetzungen
- **Phantombite Core** (Commands und Performance-Steuerung)

## Bekannte Probleme
- Ein Totalabsturz durch Radioaktivität ist bekannt und wird untersucht.

Workshop-ID: 3728225683
