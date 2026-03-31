using System.Collections.Generic;
using TokuTactics.Core.Grid;

namespace TokuTactics.Data.Content
{
    /// <summary>
    /// Definition of a complete episode — one "mission" in the campaign.
    /// An episode is a self-contained container of phases, enemies, and story.
    /// Building a new episode should feel like filling out a template.
    /// 
    /// Separation of concerns:
    /// - EpisodeDefinition owns WHAT happens: spawns, objectives, cutscenes, story triggers.
    /// - MapDefinition owns WHERE it happens: terrain, elevation, geometry.
    /// - The map provides Ranger spawn positions (fixed per map layout).
    /// - The episode provides enemy spawns (vary per episode on the same map).
    /// </summary>
    public class EpisodeDefinition
    {
        /// <summary>Unique identifier.</summary>
        public string Id { get; set; }

        /// <summary>Display title.</summary>
        public string Title { get; set; }

        /// <summary>Map ID to load for this episode's ground phase.</summary>
        public string MapId { get; set; }

        /// <summary>
        /// Ordered list of phases. Executed in sequence.
        /// Phase remixing (post mid-game) simply means non-standard ordering.
        /// Currently an ordered array; designed for expansion to a phase graph in sequels.
        /// </summary>
        public List<PhaseDefinition> Phases { get; set; } = new();

        /// <summary>Which Rangers are available for this episode (for branching character episodes).</summary>
        public List<string> AvailableRangerIds { get; set; } = new();

        /// <summary>Whether this episode is a character-focused branch (never remixed).</summary>
        public bool IsCharacterEpisode { get; set; }

        /// <summary>Whether this episode is part of the main trunk (post-convergence).</summary>
        public bool IsMainTrunk { get; set; }

        /// <summary>Prerequisite episodes that must be completed before this one unlocks.</summary>
        public List<string> Prerequisites { get; set; } = new();

        /// <summary>Story moment triggers — form budget expansion, mechanic introductions, etc.</summary>
        public List<StoryTrigger> StoryTriggers { get; set; } = new();

        /// <summary>Bond tier requirement to unlock (for tier 4 side missions).</summary>
        public BondRequirement BondRequirement { get; set; }

        /// <summary>
        /// Enemy IDs that must be killed to win (bosses/monsters).
        /// If empty, defaults to all enemies.
        /// </summary>
        public List<string> DefeatTargetIds { get; set; } = new();
    }

    /// <summary>
    /// Definition of a single phase within an episode.
    /// Phases are self-contained — no implicit knowledge of their position in sequence.
    /// </summary>
    public class PhaseDefinition
    {
        /// <summary>Unique identifier for this phase.</summary>
        public string Id { get; set; }

        /// <summary>What type of combat this phase uses.</summary>
        public PhaseType Type { get; set; }

        /// <summary>Enemy spawns for this phase.</summary>
        public List<EnemySpawnEntry> EnemySpawns { get; set; } = new();

        /// <summary>Cutscene IDs to play before and after this phase.</summary>
        public PhaseCutscenes Cutscenes { get; set; } = new();

        /// <summary>Whether cooldowns carry over from the previous phase.</summary>
        public bool CarryCooldowns { get; set; }
    }

    public enum PhaseType
    {
        /// <summary>Unmorphed + morphed ground combat.</summary>
        Ground,

        /// <summary>Zord + Megazord combat.</summary>
        Mecha,

        /// <summary>Pure cutscene phase (no combat).</summary>
        Narrative
    }

    // === Supporting Data Types ===

    public class EnemySpawnEntry
    {
        /// <summary>Enemy data ID from the EnemyDataRegistry.</summary>
        public string EnemyDataId { get; set; }

        /// <summary>Unique instance ID for this spawn (e.g., "putty_1").</summary>
        public string InstanceId { get; set; }

        /// <summary>Grid position to spawn at.</summary>
        public GridPosition Position { get; set; }

        /// <summary>Turn on which this enemy appears (0 = start of phase).</summary>
        public int SpawnTurn { get; set; }
    }

    public class PhaseCutscenes
    {
        public string BeforeCutsceneId { get; set; }
        public string AfterCutsceneId { get; set; }

        /// <summary>Mid-phase cutscenes triggered by conditions.</summary>
        public List<ConditionalCutscene> ConditionalCutscenes { get; set; } = new();
    }

    public class ConditionalCutscene
    {
        public string CutsceneId { get; set; }

        /// <summary>Condition string evaluated by the game layer (e.g., "enemy_defeated:wyrm_1").</summary>
        public string TriggerCondition { get; set; }
    }

    public class StoryTrigger
    {
        /// <summary>Type: "expand_form_budget", "introduce_mechanic", "unlock_battleizer", etc.</summary>
        public string Type { get; set; }
        public string Value { get; set; }

        /// <summary>When: "on_complete", "on_phase_start:ground_1", etc.</summary>
        public string Condition { get; set; }
    }

    public class BondRequirement
    {
        public string RangerA { get; set; }
        public string RangerB { get; set; }
        public int RequiredTier { get; set; }
    }
}
