using System.Collections.Generic;
using TokuTactics.Core.Grid;
using TokuTactics.Entities.Weapons;

namespace TokuTactics.Commands.Gimmick
{
    /// <summary>
    /// Concrete resolution of a gimmick — lists exactly what happens to whom.
    /// Lives in the Commands layer so commands can produce it without
    /// referencing the orchestrator namespace.
    /// </summary>
    public class GimmickResolution
    {
        public List<DamageEffect> DamageEffects { get; } = new();
        public List<StatusEffectApplication> StatusEffects { get; } = new();
        public List<DisplacementEffect> Displacements { get; } = new();
        public List<TerrainChange> TerrainChanges { get; } = new();
        public List<SpawnEffect> Spawns { get; } = new();
        public float OwnerHealing { get; set; }
        public bool ActivateShield { get; set; }
        public int ShieldDuration { get; set; }

        public bool HasEffects => DamageEffects.Count > 0 || StatusEffects.Count > 0
            || Displacements.Count > 0 || TerrainChanges.Count > 0
            || Spawns.Count > 0 || OwnerHealing > 0 || ActivateShield;

        public int TotalEffectCount => DamageEffects.Count + StatusEffects.Count
            + Displacements.Count + TerrainChanges.Count + Spawns.Count
            + (OwnerHealing > 0 ? 1 : 0) + (ActivateShield ? 1 : 0);

        public static GimmickResolution Empty => new GimmickResolution();
    }

    public class DamageEffect
    {
        public string TargetId { get; set; }
        public float Damage { get; set; }
    }

    public class StatusEffectApplication
    {
        public string TargetId { get; set; }
        public StatusEffectTemplate Template { get; set; }
    }

    public class DisplacementEffect
    {
        public string TargetId { get; set; }
        public GridPosition From { get; set; }
        public GridPosition To { get; set; }
    }

    public class TerrainChange
    {
        public GridPosition Position { get; set; }
        public TerrainType NewTerrain { get; set; }
    }

    public class SpawnEffect
    {
        public GridPosition Position { get; set; }
        public string EnemyDataId { get; set; }
    }
}
