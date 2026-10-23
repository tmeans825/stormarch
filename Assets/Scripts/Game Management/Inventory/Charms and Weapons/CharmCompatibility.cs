using System.Collections.Generic;

public static class CharmCompatibility
{
    // Each inner array is a mutually exclusive set. At most one member may appear on the same item.
    private static readonly string[][] exclusionGroups = new string[][]
    {
        new[] { "tiny_bullets", "big_bullets", "multi_pellet", "fork_shot" },
        new[] { "explosive", "charge_laser" },
        new[] { "slow_fire", "faster_shots", "slow_spool" },
        new[] { "reinforcement_lattice", "flexi_belt" },
    };

    private static readonly Dictionary<string, HashSet<string>> conflictsById = buildIndex();

    private static Dictionary<string, HashSet<string>> buildIndex()
    {
        var map = new Dictionary<string, HashSet<string>>();
        foreach (var group in exclusionGroups)
            foreach (var id in group)
            {
                if (!map.TryGetValue(id, out var set))
                    map[id] = set = new HashSet<string>();
                foreach (var other in group)
                    if (other != id) set.Add(other);
            }
        return map;
    }

    public static bool isCompatibleWithSelection(Charm candidate, List<Charm> selected)
    {
        if (!conflictsById.TryGetValue(candidate.id, out var conflicts)) return true;
        for (int i = 0; i < selected.Count; i++)
            if (conflicts.Contains(selected[i].id)) return false;
        return true;
    }

    public static IEnumerable<string> allReferencedIds() => conflictsById.Keys;
}
