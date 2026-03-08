using System;

namespace Server.Custom.UORespawnServer.Entities;
internal struct StatEntity
{
    internal DateTime Time { get; set; }

    internal string Player { get; set; }
    internal string Spawn { get; set; }

    internal int MapId { get; set; }
    internal int P_LocX { get; set; }
    internal int P_LocY { get; set; }
    internal int S_LocX { get; set; }
    internal int S_LocY { get; set; }

    public override string ToString()
    {
        var p_LocString = $"{P_LocX}|{P_LocY}";
        var s_LocString = $"{S_LocX}|{S_LocY}";

        return $"{Time:t}|{Player}|{MapId}|{p_LocString}|{s_LocString}|{Spawn}" + Environment.NewLine;
    }
}
