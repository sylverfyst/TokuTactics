using TokuTactics.Core.Grid;

namespace TokuTactics.Data.Content
{
    /// <summary>
    /// Vertical slice episode definitions.
    /// 
    /// Each episode is a template: map + spawns + objectives + cutscenes.
    /// Building a new episode = filling out this template with content IDs.
    /// No new systems, no new code — just data.
    /// </summary>
    public static class EpisodeCatalog
    {
        /// <summary>
        /// "Frozen Outpost" — mid-campaign episode.
        /// 
        /// Ground phase: 5 foot soldiers (3 Putty, 2 Blaze Grunt) in the middle,
        /// Frost Wyrm on northern high ground, Shadow Commander on the east flank.
        /// 
        /// Win condition: defeat the Frost Wyrm and Shadow Commander.
        /// Foot soldiers are optional (but dangerous if ignored).
        /// </summary>
        public static EpisodeDefinition FrozenOutpostEpisode()
        {
            return new EpisodeDefinition
            {
                Id = "episode_frozen_outpost",
                Title = "The Frozen Outpost",
                MapId = "map_frozen_outpost",
                AvailableRangerIds = { "ranger_red", "ranger_blue", "ranger_yellow",
                    "ranger_green", "ranger_pink" },
                IsMainTrunk = true,
                DefeatTargetIds = { "wyrm_1", "commander_1" },
                Phases =
                {
                    new PhaseDefinition
                    {
                        Id = "ground_1",
                        Type = PhaseType.Ground,
                        Cutscenes = new PhaseCutscenes
                        {
                            BeforeCutsceneId = "cutscene_frozen_outpost_intro"
                        },
                        EnemySpawns =
                        {
                            // Foot soldiers — middle area
                            new EnemySpawnEntry
                            {
                                EnemyDataId = "foot_putty",
                                InstanceId = "putty_1",
                                Position = new GridPosition(4, 5)
                            },
                            new EnemySpawnEntry
                            {
                                EnemyDataId = "foot_putty",
                                InstanceId = "putty_2",
                                Position = new GridPosition(7, 5)
                            },
                            new EnemySpawnEntry
                            {
                                EnemyDataId = "foot_putty",
                                InstanceId = "putty_3",
                                Position = new GridPosition(5, 6)
                            },
                            new EnemySpawnEntry
                            {
                                EnemyDataId = "foot_blaze_grunt",
                                InstanceId = "grunt_1",
                                Position = new GridPosition(8, 5)
                            },
                            new EnemySpawnEntry
                            {
                                EnemyDataId = "foot_blaze_grunt",
                                InstanceId = "grunt_2",
                                Position = new GridPosition(3, 6)
                            },

                            // Monster — northern high ground
                            new EnemySpawnEntry
                            {
                                EnemyDataId = "monster_frost_wyrm",
                                InstanceId = "wyrm_1",
                                Position = new GridPosition(6, 1)
                            },

                            // Lieutenant — eastern flank
                            new EnemySpawnEntry
                            {
                                EnemyDataId = "lt_shadow_commander",
                                InstanceId = "commander_1",
                                Position = new GridPosition(10, 2)
                            }
                        }
                    }
                }
            };
        }
    }
}
