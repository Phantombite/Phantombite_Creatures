using System.Collections.Generic;

namespace PhantombiteCreatures.Modules
{
    public class CreatureDefinition
    {
        public string SubtypeId;
        public bool   Enabled;

        // Planeten (leer = kein Spawn)
        public HashSet<string> DayPlanets   = new HashSet<string>();
        public HashSet<string> NightPlanets = new HashSet<string>();

        // Spawn-Limit — nur pro Spieler, kein GlobalMax mehr
        public int    PerPlayerMax       = 3;

        // Spawn-Distanz
        public double MinSpawnRadius     = 500;
        public double SpawnRadius        = 2000;
        public double MinCreatureSpacing = 300;

        // Pack-Größe (Tag / Nacht getrennt)
        public int PackMin      = 1;
        public int PackMax      = 1;
        public int NightPackMin = 1;
        public int NightPackMax = 1;

        public bool CanSpawnOnPlanet(string planetName, bool isDay)
        {
            return isDay ? DayPlanets.Contains(planetName) : NightPlanets.Contains(planetName);
        }
    }
}