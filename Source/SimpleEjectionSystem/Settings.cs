namespace SimpleEjectionSystem
{
    internal class Settings
    {
        // Normal: 1, Harder: 2, Darkest: 3 
        public int Difficulty = 1;
        public float EjectionChanceMax = 25f;
        public float PointlessEjectionChance = 90f;

        // Exits
        public bool PlayerCharacterAlwaysResists = true;
        public bool ElitePilotsAlwaysResists = true;
        public bool KnockedDownCannotEject = true;

        // Resists
        public float BaseEjectionResist = 5;
        public float GutsEjectionResistPerPoint = 2;
        public float GutsTenAddEjectionResist = 5;
        public float InspiredEjectionResist = 2;
        public float PilotHighMoraleEjectionResist = 2;
        public float CommanderFieldedEjectionResist = 3;

        // Additional resists for inbetween attacks
        public float PilotStillAtFullHealthModifier = 2;
        public float MechStillAtGoodHealthModifier = 2;

        // Static Modifiers
        public float PilotLowMoraleModifier = 2;
        public float PilotHealthMaxModifier = 5;
        public float PilotHealthOneAddModifier = 3;
        public float HeadDamageMaxModifier = 5;
        public float CTDamageMaxModifier = 10;
        public float SideTorsoInternalDamageMaxModifier = 5;
        public float LeggedMaxModifier = 10;
        public float WeaponlessModifier = 15;
        public float OutnumberedPerMechModifier = 1;
        public float AloneModifier = 5;
        public float StressPerLevelModifier = 1;

        // Situation dependent modifiers (Spikes)
        public float UnsteadyModifier = 3;
        public float AttackDestroyedAnyLocationModifier = 5;
        public float AttackCausedAmmoExplosionModifier = 3;
        public float AttackDestroyedWeaponModifier = 2;
        public float NextShotLikeThatCouldKillModifier = 10;
    }
}
