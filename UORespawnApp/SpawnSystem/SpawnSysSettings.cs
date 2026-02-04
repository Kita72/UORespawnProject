namespace Server.Custom.SpawnSystem
{
    internal static class SpawnSysSettings
    {
        internal static int INTERVAL { get; private set; } = 50;

        internal static int MARKER { get; private set; } = 72;

        internal static int MIN_QUE { get; private set; } = 1;

        internal static int MAX_MOBS { get; private set; } = 15;

        internal static int MIN_RANGE { get; private set; } = 10;

        internal static int MAX_RANGE { get; private set; } = 50;

        internal static int MAX_CROWD { get; private set; } = 1;

        internal static double CHANCE_ISWATER { get; private set; } = 0.5;

        internal static double CHANCE_ISWeather { get; private set; } = 0.1;

        internal static double CHANCE_ISStatic { get; private set; } = 0.1;

        internal static bool SCALE_SPAWN { get; private set; } = false;

        internal static double SPAWN_MOD { get; private set; } = 0.0;

        internal static void InitializeStats()
        {
            MAX_MOBS = GetSpawnMod(SpawnSysDataBase.MaxMobs);
            MIN_RANGE = GetSpawnMod(SpawnSysDataBase.MinRange);
            MAX_RANGE = GetSpawnMod(SpawnSysDataBase.MaxRange);
            MAX_CROWD = GetSpawnMod(SpawnSysDataBase.MaxCrowd);

            CHANCE_ISWATER = SpawnSysDataBase.WaterChance;
            CHANCE_ISWeather = SpawnSysDataBase.WeatherChance;
            CHANCE_ISStatic = SpawnSysDataBase.StaticChance;

            SCALE_SPAWN = SpawnSysDataBase.ScaleSpawn;
        }

        internal static void UpdateStats(double mod)
        {
            SPAWN_MOD = mod;

            MAX_MOBS = GetSpawnMod(SpawnSysDataBase.MaxMobs);
            MIN_RANGE = GetSpawnMod(SpawnSysDataBase.MinRange);
            MAX_RANGE = GetSpawnMod(SpawnSysDataBase.MaxRange);
            MAX_CROWD = GetSpawnMod(SpawnSysDataBase.MaxCrowd);
        }

        internal static int GetSpawnMod(int stat)
        {
            if (SPAWN_MOD > 0)
            {
                double result = stat * SPAWN_MOD;

                return (int)result + stat;
            }

            return stat;
        }
    }
}
