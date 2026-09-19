# Phantombite Creatures

Lässt **Kreaturen** in der Nähe von Spielern spawnen: Wölfe und Weltraum-Spinnen, mit eigenen Regeln pro Planet und Tageszeit.
Kein MES nötig, das Spawn-System ist eigener Code.

## Funktionen
| Kreatur | Wo / Wann (Standard) |
|---|---|
| Wolf | EarthLike, Tag 1–2, Nacht 2–4 |
| SpaceSpider, SpaceSpiderBrown, SpaceSpiderBlack | Alien und Pertam, Tag 1–4 |

- Spawn in der Nähe von Spielern mit Abklingzeit, automatisches Aufräumen zu weit entfernter Kreaturen und von Leichen
- Alles in einer Config einstellbar
- Reagiert auf die Server-Last: bei Überlast halbe Rate, bei sehr hoher Last aus (über den Phantombite Core)

## Commands
```
!pbc creatures <command>
```
| Command | Wer | Beschreibung |
|---|---|---|
| `status` | alle | Aktive Kreaturen und Wellen anzeigen |
| `spawn [wolf\|spider\|spiderbrown\|spiderblack]` | Admin | Spawn-Timer zurücksetzen bzw. einen Typ spawnen |
| `timer [Spielername]` | Admin | Timer für einen Spieler zurücksetzen |

## Konfiguration
`PhantombiteCreatures_Config.ini` im World-Storage (wird beim ersten Start mit Standardwerten angelegt, ein Abschnitt pro Kreatur).

## Voraussetzungen
- **Phantombite Core** (Commands und Performance-Steuerung)

## Bekannte Probleme
- Ein Totalabsturz durch Radioaktivität ist bekannt und wird untersucht.

Workshop-ID: 3728225683
