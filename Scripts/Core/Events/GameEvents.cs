using TokuTactics.Core.Types;

namespace TokuTactics.Core.Events
{
    // =========================================================================
    // GAME STATE EVENTS (Priority: GameState)
    // These modify core game state. Dispatched first so all systems read correct data.
    // =========================================================================

    /// <summary>A form's health reached 0. Ranger has been demorphed.</summary>
    public class FormDiedEvent : IGameEvent
    {
        public EventPriority Priority => EventPriority.GameState;
        public string RangerId { get; set; }
        public string FormId { get; set; }
        public bool IsPermadeath { get; set; }
    }

    /// <summary>A Ranger was forcibly demorphed (from form death).</summary>
    public class RangerDemorphedEvent : IGameEvent
    {
        public EventPriority Priority => EventPriority.GameState;
        public string RangerId { get; set; }
        public string LostFormId { get; set; }
    }

    /// <summary>An unmorphed Ranger's health reached 0. Mission is lost.</summary>
    public class RangerDiedUnmorphedEvent : IGameEvent
    {
        public EventPriority Priority => EventPriority.GameState;
        public string RangerId { get; set; }
    }

    /// <summary>A form was vacated and its cooldown started.</summary>
    public class FormCooldownStartedEvent : IGameEvent
    {
        public EventPriority Priority => EventPriority.GameState;
        public string FormId { get; set; }
        public string RangerId { get; set; }
        public int Duration { get; set; }
    }

    /// <summary>A form's cooldown expired and it's available again.</summary>
    public class FormCooldownExpiredEvent : IGameEvent
    {
        public EventPriority Priority => EventPriority.GameState;
        public string FormId { get; set; }
    }

    /// <summary>A Ranger morphed into base form.</summary>
    public class RangerMorphedEvent : IGameEvent
    {
        public EventPriority Priority => EventPriority.GameState;
        public string RangerId { get; set; }
        public bool IsFirstMorphThisMission { get; set; }
    }

    /// <summary>A Ranger switched forms.</summary>
    public class FormSwitchedEvent : IGameEvent
    {
        public EventPriority Priority => EventPriority.GameState;
        public string RangerId { get; set; }
        public string PreviousFormId { get; set; }
        public string NewFormId { get; set; }
        public int ChainPosition { get; set; }
    }

    /// <summary>A status effect was applied to an entity.</summary>
    public class StatusEffectAppliedEvent : IGameEvent
    {
        public EventPriority Priority => EventPriority.GameState;
        public string TargetId { get; set; }
        public string EffectId { get; set; }
        public int Duration { get; set; }
    }

    /// <summary>A status effect expired or was removed.</summary>
    public class StatusEffectRemovedEvent : IGameEvent
    {
        public EventPriority Priority => EventPriority.GameState;
        public string TargetId { get; set; }
        public string EffectId { get; set; }
    }

    /// <summary>Megazord combination activated.</summary>
    public class MegazordCombinedEvent : IGameEvent
    {
        public EventPriority Priority => EventPriority.GameState;
        public int TimerDuration { get; set; }
    }

    /// <summary>Megazord combination ended (timer or ultimate).</summary>
    public class MegazordDecombinedEvent : IGameEvent
    {
        public EventPriority Priority => EventPriority.GameState;
        public bool WasUltimate { get; set; }
        public float UltimateMultiplier { get; set; }
    }

    /// <summary>A zord was hot-swapped during Megazord combination.</summary>
    public class ZordSwappedEvent : IGameEvent
    {
        public EventPriority Priority => EventPriority.GameState;
        public int SlotIndex { get; set; }
        public string RemovedZordId { get; set; }
        public string NewZordId { get; set; }
    }

    /// <summary>Battleizer activated.</summary>
    public class BattleizerActivatedEvent : IGameEvent
    {
        public EventPriority Priority => EventPriority.GameState;
        public string RangerId { get; set; }
        public int Duration { get; set; }
    }

    /// <summary>Battleizer window expired.</summary>
    public class BattleizerExpiredEvent : IGameEvent
    {
        public EventPriority Priority => EventPriority.GameState;
        public string RangerId { get; set; }
    }

    /// <summary>The team loadout was selected and locked.</summary>
    public class LoadoutLockedEvent : IGameEvent
    {
        public EventPriority Priority => EventPriority.GameState;
        public string[] EquippedFormIds { get; set; }
    }

    // =========================================================================
    // GAMEPLAY EVENTS (Priority: Gameplay)
    // These affect gameplay systems but don't change core state.
    // =========================================================================

    /// <summary>Damage was dealt to a target.</summary>
    public class DamageDealtEvent : IGameEvent
    {
        public EventPriority Priority => EventPriority.Gameplay;
        public string AttackerId { get; set; }
        public string TargetId { get; set; }
        public int Amount { get; set; }
        public bool WasCritical { get; set; }
        public bool WasDodged { get; set; }
        public MatchupResult TypeMatchup { get; set; }
        public bool HadSameTypeBonus { get; set; }
        public float ComboMultiplier { get; set; }
    }

    /// <summary>An assist occurred between two Rangers.</summary>
    public class AssistOccurredEvent : IGameEvent
    {
        public EventPriority Priority => EventPriority.Gameplay;
        public string ActorId { get; set; }
        public string AssisterId { get; set; }
        public int BondTier { get; set; }
        public bool TriggeredPairAttack { get; set; }
    }

    /// <summary>A bond reached a new tier.</summary>
    public class BondTierReachedEvent : IGameEvent
    {
        public EventPriority Priority => EventPriority.Gameplay;
        public string RangerA { get; set; }
        public string RangerB { get; set; }
        public int OldTier { get; set; }
        public int NewTier { get; set; }
    }

    /// <summary>A tier 2 bond pair attack pulled the assister to base form.</summary>
    public class Tier2FormDisruptionEvent : IGameEvent
    {
        public EventPriority Priority => EventPriority.Gameplay;
        public string AssisterId { get; set; }
        public string DisruptedFormId { get; set; }
    }

    /// <summary>A tier 4 bond refresh was given.</summary>
    public class BondRefreshEvent : IGameEvent
    {
        public EventPriority Priority => EventPriority.Gameplay;
        public string GiverId { get; set; }
        public string ReceiverId { get; set; }
    }

    /// <summary>A form leveled up.</summary>
    public class FormLeveledUpEvent : IGameEvent
    {
        public EventPriority Priority => EventPriority.Gameplay;
        public string RangerId { get; set; }
        public string FormId { get; set; }
        public int NewLevel { get; set; }
        public bool ProclivityTriggered { get; set; }
    }

    /// <summary>An enemy crossed its aggression threshold.</summary>
    public class AggressionTriggeredEvent : IGameEvent
    {
        public EventPriority Priority => EventPriority.Gameplay;
        public string EnemyId { get; set; }
        public float HealthPercentage { get; set; }
    }

    /// <summary>An enemy was defeated.</summary>
    public class EnemyDefeatedEvent : IGameEvent
    {
        public EventPriority Priority => EventPriority.Gameplay;
        public string EnemyId { get; set; }
        public string EnemyTypeId { get; set; }
        public ElementalType EnemyType { get; set; }
    }

    /// <summary>The big bad's reality distortion event triggered.</summary>
    public class RealityDisruptionEvent : IGameEvent
    {
        public EventPriority Priority => EventPriority.Gameplay;
        public string DisruptionType { get; set; }
    }

    /// <summary>A phase transition is happening.</summary>
    public class PhaseTransitionEvent : IGameEvent
    {
        public EventPriority Priority => EventPriority.Gameplay;
        public string FromPhaseId { get; set; }
        public string ToPhaseId { get; set; }
        public bool CooldownsCarryOver { get; set; }
    }

    /// <summary>The player turn phase started.</summary>
    public class PlayerPhaseStartedEvent : IGameEvent
    {
        public EventPriority Priority => EventPriority.Gameplay;
        public int RoundNumber { get; set; }
    }

    /// <summary>The enemy turn phase started.</summary>
    public class EnemyPhaseStartedEvent : IGameEvent
    {
        public EventPriority Priority => EventPriority.Gameplay;
        public int RoundNumber { get; set; }
    }

    /// <summary>A new round started. Cooldowns and status effects have been ticked.</summary>
    public class RoundStartedEvent : IGameEvent
    {
        public EventPriority Priority => EventPriority.Gameplay;
        public int RoundNumber { get; set; }
    }

    /// <summary>A unit's turn started within a phase.</summary>
    public class UnitTurnStartedEvent : IGameEvent
    {
        public EventPriority Priority => EventPriority.Gameplay;
        public string UnitId { get; set; }
        public int RoundNumber { get; set; }
    }

    /// <summary>A unit's turn ended within a phase.</summary>
    public class UnitTurnEndedEvent : IGameEvent
    {
        public EventPriority Priority => EventPriority.Gameplay;
        public string UnitId { get; set; }
    }

    /// <summary>The mission was won — all required enemies defeated.</summary>
    public class MissionVictoryEvent : IGameEvent
    {
        public EventPriority Priority => EventPriority.GameState;
        public int RoundsElapsed { get; set; }
    }

    /// <summary>The mission was lost — unmorphed Ranger killed.</summary>
    public class MissionDefeatEvent : IGameEvent
    {
        public EventPriority Priority => EventPriority.GameState;
        public string FallenRangerId { get; set; }
        public int RoundsElapsed { get; set; }
    }

    // =========================================================================
    // PRESENTATION EVENTS (Priority: Presentation)
    // UI and visual responses. Read final state, never modify it.
    // These bridge to Godot signals at the presentation layer.
    // =========================================================================

    /// <summary>Request to play a morph animation sequence.</summary>
    public class PlayMorphAnimationEvent : IGameEvent
    {
        public EventPriority Priority => EventPriority.Presentation;
        public string RangerId { get; set; }
        public string FormId { get; set; }
        public bool IsFirstTimeForThisForm { get; set; }
    }

    /// <summary>Request to show the battle screen for an attack.</summary>
    public class ShowBattleScreenEvent : IGameEvent
    {
        public EventPriority Priority => EventPriority.Presentation;
        public string AttackerId { get; set; }
        public string TargetId { get; set; }
        public string AttackType { get; set; }
        public int DamageDealt { get; set; }
        public bool WasCritical { get; set; }
        public string AssisterId { get; set; }
    }

    /// <summary>Request to play Megazord combination sequence.</summary>
    public class PlayCombinationSequenceEvent : IGameEvent
    {
        public EventPriority Priority => EventPriority.Presentation;
    }

    /// <summary>Request to play Megazord ultimate attack sequence.</summary>
    public class PlayUltimateSequenceEvent : IGameEvent
    {
        public EventPriority Priority => EventPriority.Presentation;
        public float Multiplier { get; set; }
    }

    /// <summary>Request to play Battleizer activation sequence.</summary>
    public class PlayBattleizerSequenceEvent : IGameEvent
    {
        public EventPriority Priority => EventPriority.Presentation;
        public string RangerId { get; set; }
    }

    /// <summary>Request to show a cutscene.</summary>
    public class TriggerCutsceneEvent : IGameEvent
    {
        public EventPriority Priority => EventPriority.Presentation;
        public string CutsceneId { get; set; }
    }

    /// <summary>Request to show in-character dialogue (e.g., tier 2 bond reaction).</summary>
    public class ShowDialogueEvent : IGameEvent
    {
        public EventPriority Priority => EventPriority.Presentation;
        public string SpeakerId { get; set; }
        public string DialogueKey { get; set; }
    }

    // =========================================================================
    // AUDIO EVENTS (Priority: Audio)
    // Sound effects and music. Always dispatched last.
    // =========================================================================

    /// <summary>Request to play a sound effect.</summary>
    public class PlaySoundEvent : IGameEvent
    {
        public EventPriority Priority => EventPriority.Audio;
        public string SoundId { get; set; }
    }

    /// <summary>Request to change background music.</summary>
    public class ChangeMusicEvent : IGameEvent
    {
        public EventPriority Priority => EventPriority.Audio;
        public string TrackId { get; set; }
        public bool FadeIn { get; set; }
    }
}
