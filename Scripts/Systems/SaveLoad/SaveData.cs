using System.Collections.Generic;

namespace TokuTactics.Systems.SaveLoad
{
    /// <summary>
    /// Root save data object. Contains everything needed to restore the game.
    /// This is a plain data class with no logic — it exists purely for serialization.
    /// 
    /// Two save contexts:
    /// - Between episodes: CampaignData is populated, MissionSnapshot is null.
    /// - Mid-episode restore point: both CampaignData and MissionSnapshot are populated.
    /// 
    /// The game layer converts runtime objects to/from this structure.
    /// The save system only knows how to persist and restore SaveData.
    /// </summary>
    public class SaveData
    {
        /// <summary>Save format version. Increment when save structure changes.</summary>
        public int Version { get; set; } = 1;

        /// <summary>Display name for this save slot (e.g., "Episode 5 — Before Boss").</summary>
        public string SlotName { get; set; }

        /// <summary>Timestamp when this save was created.</summary>
        public string Timestamp { get; set; }

        /// <summary>Total play time in seconds.</summary>
        public double PlayTimeSeconds { get; set; }

        /// <summary>Campaign-level persistent state.</summary>
        public CampaignData Campaign { get; set; }

        /// <summary>
        /// Mid-mission snapshot for the restore point.
        /// Null when saving between episodes.
        /// </summary>
        public MissionSnapshotData MissionSnapshot { get; set; }
    }

    // === Campaign Data ===

    /// <summary>
    /// All persistent state that carries between episodes.
    /// </summary>
    public class CampaignData
    {
        /// <summary>Which playthrough: 0 = first, 1 = NG+, 2 = NG++.</summary>
        public int NewGamePlusCount { get; set; }

        /// <summary>ID of the last completed episode.</summary>
        public string LastCompletedEpisodeId { get; set; }

        /// <summary>IDs of all completed episodes.</summary>
        public List<string> CompletedEpisodeIds { get; set; } = new();

        /// <summary>Current form budget for the core five.</summary>
        public int FormBudget { get; set; }

        /// <summary>Per-Ranger persistent data.</summary>
        public List<RangerSaveData> Rangers { get; set; } = new();

        /// <summary>Bond experience between Ranger pairs.</summary>
        public List<BondSaveData> Bonds { get; set; } = new();

        /// <summary>Zord collection and levels.</summary>
        public List<ZordSaveData> Zords { get; set; } = new();

        /// <summary>Whether the 6th Ranger has been recruited.</summary>
        public bool SixthRangerRecruited { get; set; }

        /// <summary>6th Ranger form budget (separate from core five).</summary>
        public int SixthRangerFormBudget { get; set; }

        /// <summary>IDs of forms permanently lost to permadeath.</summary>
        public List<string> LostFormIds { get; set; } = new();

        /// <summary>Whether the Battleizer has been unlocked.</summary>
        public bool BattleizerUnlocked { get; set; }

        /// <summary>ID of the Ranger who received the Battleizer.</summary>
        public string BattleizerRangerId { get; set; }
    }

    /// <summary>
    /// Per-Ranger persistent data.
    /// </summary>
    public class RangerSaveData
    {
        public string RangerId { get; set; }

        /// <summary>Proclivity stat assignment (randomized on first save, persists through NG+).</summary>
        public string ProclivityStat { get; set; }

        /// <summary>Per-form level and experience.</summary>
        public List<FormLevelData> FormLevels { get; set; } = new();
    }

    /// <summary>
    /// Per-Ranger, per-form level data.
    /// </summary>
    public class FormLevelData
    {
        public string FormId { get; set; }
        public int Level { get; set; }
        public int Experience { get; set; }
    }

    /// <summary>
    /// Bond state between two Rangers.
    /// </summary>
    public class BondSaveData
    {
        public string RangerAId { get; set; }
        public string RangerBId { get; set; }
        public int Experience { get; set; }
        public int Tier { get; set; }
    }

    /// <summary>
    /// Zord collection entry.
    /// </summary>
    public class ZordSaveData
    {
        public string ZordId { get; set; }
        public int Level { get; set; }
        public int Experience { get; set; }
        public int RecruitmentOrder { get; set; }
    }

    // === Mission Snapshot ===

    /// <summary>
    /// Complete snapshot of an in-progress mission.
    /// Used for the single mid-episode restore point.
    /// </summary>
    public class MissionSnapshotData
    {
        /// <summary>ID of the episode/mission in progress.</summary>
        public string EpisodeId { get; set; }

        /// <summary>Current round number.</summary>
        public int RoundNumber { get; set; }

        /// <summary>Current phase (Player, Enemy, Idle).</summary>
        public string PhaseState { get; set; }

        /// <summary>ID of the unit whose turn it is (null if between turns).</summary>
        public string ActiveUnitId { get; set; }

        /// <summary>Grid dimensions.</summary>
        public int GridWidth { get; set; }
        public int GridHeight { get; set; }

        /// <summary>Per-tile terrain overrides from the base map (gimmick terrain changes).</summary>
        public List<TileSaveData> ModifiedTiles { get; set; } = new();

        /// <summary>Ranger state snapshots.</summary>
        public List<RangerSnapshotData> Rangers { get; set; } = new();

        /// <summary>Enemy state snapshots.</summary>
        public List<EnemySnapshotData> Enemies { get; set; } = new();

        /// <summary>Form pool state.</summary>
        public FormPoolSnapshotData FormPool { get; set; }

        /// <summary>Scouting intelligence gathered so far.</summary>
        public ScoutingSnapshotData Scouting { get; set; }

        /// <summary>Whether the loadout has been locked.</summary>
        public bool LoadoutLocked { get; set; }

        /// <summary>IDs of equipped forms.</summary>
        public List<string> EquippedFormIds { get; set; } = new();
    }

    /// <summary>
    /// Tile state for terrain that was modified during the mission.
    /// Only tiles that differ from the base map definition are saved.
    /// </summary>
    public class TileSaveData
    {
        public int Col { get; set; }
        public int Row { get; set; }
        public string Terrain { get; set; }
    }

    /// <summary>
    /// Ranger in-mission state.
    /// </summary>
    public class RangerSnapshotData
    {
        public string RangerId { get; set; }
        public int Col { get; set; }
        public int Row { get; set; }

        /// <summary>Unmorphed, Morphed, or Demorphed.</summary>
        public string MorphState { get; set; }

        /// <summary>Current form ID (null if unmorphed/demorphed).</summary>
        public string CurrentFormId { get; set; }

        /// <summary>Unmorphed health.</summary>
        public float UnmorphedHealth { get; set; }

        /// <summary>Per-form health snapshots (forms that have been used this mission).</summary>
        public List<FormHealthData> FormHealths { get; set; } = new();

        /// <summary>Active status effects.</summary>
        public List<StatusEffectSaveData> StatusEffects { get; set; } = new();

        /// <summary>Combo chain count.</summary>
        public int ComboChainCount { get; set; }

        /// <summary>Whether this Ranger has used their bond refresh this round.</summary>
        public bool HasUsedBondRefresh { get; set; }

        /// <summary>Whether this Ranger has received a bond refresh this round.</summary>
        public bool HasReceivedBondRefresh { get; set; }
    }

    public class FormHealthData
    {
        public string FormId { get; set; }
        public float CurrentHealth { get; set; }
        public float MaxHealth { get; set; }
    }

    /// <summary>
    /// Enemy in-mission state.
    /// </summary>
    public class EnemySnapshotData
    {
        public string InstanceId { get; set; }
        public string DataId { get; set; }
        public int Col { get; set; }
        public int Row { get; set; }
        public float CurrentHealth { get; set; }
        public bool IsAggressive { get; set; }

        /// <summary>Active status effects.</summary>
        public List<StatusEffectSaveData> StatusEffects { get; set; } = new();

        /// <summary>Gimmick-specific state (turns since last activation, etc.).</summary>
        public int GimmickCooldownRemaining { get; set; }

        /// <summary>Which action types have been used this turn.</summary>
        public List<string> UsedActionTypes { get; set; } = new();
    }

    /// <summary>
    /// Serialized status effect. Enough to reconstruct the StatusEffectInstance.
    /// </summary>
    public class StatusEffectSaveData
    {
        public string EffectId { get; set; }
        public string TriggerId { get; set; }
        public string BehaviorId { get; set; }
        public int RemainingDuration { get; set; }
        public float Potency { get; set; }
    }

    /// <summary>
    /// Form pool state within a mission.
    /// </summary>
    public class FormPoolSnapshotData
    {
        /// <summary>Per-form cooldown remaining turns.</summary>
        public List<FormCooldownData> Cooldowns { get; set; } = new();

        /// <summary>Which Ranger occupies which form.</summary>
        public List<FormOccupancyData> Occupancies { get; set; } = new();
    }

    public class FormCooldownData
    {
        public string FormId { get; set; }
        public int RemainingTurns { get; set; }
    }

    public class FormOccupancyData
    {
        public string FormId { get; set; }
        public string RangerId { get; set; }
    }

    /// <summary>
    /// Scouting intelligence snapshot.
    /// </summary>
    public class ScoutingSnapshotData
    {
        public List<RevealedTypeSaveData> RevealedTypes { get; set; } = new();
        public List<string> ObservedEnemyIds { get; set; } = new();
    }

    public class RevealedTypeSaveData
    {
        public string EnemyId { get; set; }
        public string Type { get; set; }
    }
}
