using System.Collections.Generic;
using System.Linq;
using TokuTactics.Systems.SaveLoad;

namespace TokuTactics.Tests.Systems.SaveLoad
{
    public class SaveManagerTests
    {
        // === Helpers ===

        private SaveManager MakeManager()
        {
            return new SaveManager(new MemorySaveStorage(), new PassthroughSaveSerializer());
        }

        private SaveData MakeCampaignSave(string name = "Test Save")
        {
            return new SaveData
            {
                SlotName = name,
                PlayTimeSeconds = 3600,
                Campaign = new CampaignData
                {
                    FormBudget = 3,
                    LastCompletedEpisodeId = "episode_5",
                    CompletedEpisodeIds = new List<string> { "episode_1", "episode_5" },
                    Rangers = new List<RangerSaveData>
                    {
                        new RangerSaveData
                        {
                            RangerId = "ranger_red",
                            ProclivityStat = "STR",
                            FormLevels = new List<FormLevelData>
                            {
                                new FormLevelData { FormId = "form_base", Level = 3, Experience = 40 },
                                new FormLevelData { FormId = "form_blaze", Level = 2, Experience = 15 }
                            }
                        }
                    },
                    Bonds = new List<BondSaveData>
                    {
                        new BondSaveData
                        {
                            RangerAId = "ranger_red",
                            RangerBId = "ranger_blue",
                            Experience = 80,
                            Tier = 1
                        }
                    }
                }
            };
        }

        private SaveData MakeMissionSave()
        {
            var save = MakeCampaignSave("Mid-Mission");
            save.MissionSnapshot = new MissionSnapshotData
            {
                EpisodeId = "episode_6",
                RoundNumber = 4,
                PhaseState = "PlayerPhase",
                GridWidth = 12,
                GridHeight = 10,
                LoadoutLocked = true,
                EquippedFormIds = new List<string> { "form_blaze", "form_torrent" },
                Rangers = new List<RangerSnapshotData>
                {
                    new RangerSnapshotData
                    {
                        RangerId = "ranger_red",
                        Col = 5,
                        Row = 5,
                        MorphState = "Morphed",
                        CurrentFormId = "form_blaze",
                        UnmorphedHealth = 50f,
                        FormHealths = new List<FormHealthData>
                        {
                            new FormHealthData
                            {
                                FormId = "form_blaze",
                                CurrentHealth = 75f,
                                MaxHealth = 100f
                            }
                        }
                    }
                },
                Enemies = new List<EnemySnapshotData>
                {
                    new EnemySnapshotData
                    {
                        InstanceId = "putty_1",
                        DataId = "foot_putty",
                        Col = 5,
                        Row = 4,
                        CurrentHealth = 15f,
                        IsAggressive = true
                    }
                },
                FormPool = new FormPoolSnapshotData
                {
                    Cooldowns = new List<FormCooldownData>
                    {
                        new FormCooldownData { FormId = "form_frost", RemainingTurns = 2 }
                    },
                    Occupancies = new List<FormOccupancyData>
                    {
                        new FormOccupancyData { FormId = "form_blaze", RangerId = "ranger_red" }
                    }
                },
                Scouting = new ScoutingSnapshotData
                {
                    RevealedTypes = new List<RevealedTypeSaveData>
                    {
                        new RevealedTypeSaveData { EnemyId = "wyrm_1", Type = "Frost" }
                    },
                    ObservedEnemyIds = new List<string> { "wyrm_1", "putty_1" }
                }
            };
            return save;
        }

        // === Save Slots ===

        public void SaveAndLoad_RoundTrips()
        {
            var mgr = MakeManager();
            var save = MakeCampaignSave();

            bool saved = mgr.SaveToSlot(0, save);
            Assert(saved, "Save should succeed");

            var loaded = mgr.LoadFromSlot(0);
            Assert(loaded != null, "Load should return data");
            Assert(loaded.SlotName == "Test Save", "Name should round-trip");
            Assert(loaded.Campaign.FormBudget == 3, "Budget should round-trip");
            Assert(loaded.Campaign.Rangers.Count == 1, "Rangers should round-trip");
            Assert(loaded.Campaign.Bonds.Count == 1, "Bonds should round-trip");
        }

        public void SaveToSlot_SetsTimestamp()
        {
            var mgr = MakeManager();
            var save = MakeCampaignSave();
            save.Timestamp = null;

            mgr.SaveToSlot(0, save);

            var loaded = mgr.LoadFromSlot(0);
            Assert(!string.IsNullOrEmpty(loaded.Timestamp), "Timestamp should be set");
        }

        public void SaveToSlot_OverwritesExisting()
        {
            var mgr = MakeManager();

            mgr.SaveToSlot(0, MakeCampaignSave("First"));
            mgr.SaveToSlot(0, MakeCampaignSave("Second"));

            var loaded = mgr.LoadFromSlot(0);
            Assert(loaded.SlotName == "Second", "Should overwrite with second save");
        }

        public void LoadFromSlot_EmptyReturnsNull()
        {
            var mgr = MakeManager();

            var loaded = mgr.LoadFromSlot(0);
            Assert(loaded == null, "Empty slot should return null");
        }

        public void SaveToSlot_InvalidIndex_ReturnsFalse()
        {
            var mgr = MakeManager();

            Assert(!mgr.SaveToSlot(-1, MakeCampaignSave()), "Negative index should fail");
            Assert(!mgr.SaveToSlot(10, MakeCampaignSave()), "Out of range should fail");
        }

        public void SaveToSlot_NullData_ReturnsFalse()
        {
            var mgr = MakeManager();

            Assert(!mgr.SaveToSlot(0, null), "Null data should fail");
        }

        public void DeleteSlot_RemovesData()
        {
            var mgr = MakeManager();
            mgr.SaveToSlot(0, MakeCampaignSave());

            Assert(mgr.IsSlotOccupied(0), "Should be occupied before delete");

            bool deleted = mgr.DeleteSlot(0);
            Assert(deleted, "Delete should succeed");
            Assert(!mgr.IsSlotOccupied(0), "Should be empty after delete");
        }

        public void IsSlotOccupied_CorrectState()
        {
            var mgr = MakeManager();

            Assert(!mgr.IsSlotOccupied(0), "Empty slot should not be occupied");

            mgr.SaveToSlot(0, MakeCampaignSave());
            Assert(mgr.IsSlotOccupied(0), "Saved slot should be occupied");
        }

        public void MultipleSlots_Independent()
        {
            var mgr = MakeManager();

            mgr.SaveToSlot(0, MakeCampaignSave("Slot 0"));
            mgr.SaveToSlot(3, MakeCampaignSave("Slot 3"));

            Assert(mgr.IsSlotOccupied(0), "Slot 0 should be occupied");
            Assert(!mgr.IsSlotOccupied(1), "Slot 1 should be empty");
            Assert(mgr.IsSlotOccupied(3), "Slot 3 should be occupied");

            Assert(mgr.LoadFromSlot(0).SlotName == "Slot 0", "Slot 0 data correct");
            Assert(mgr.LoadFromSlot(3).SlotName == "Slot 3", "Slot 3 data correct");
        }

        // === Slot Info ===

        public void GetAllSlotInfo_ShowsAllSlots()
        {
            var mgr = MakeManager();
            mgr.SaveToSlot(0, MakeCampaignSave("Campaign Save"));
            mgr.SaveToSlot(2, MakeMissionSave());

            var info = mgr.GetAllSlotInfo();

            Assert(info.Count == 10, "Should have 10 slot entries");

            Assert(info[0].IsOccupied, "Slot 0 should be occupied");
            Assert(info[0].SlotName == "Campaign Save", "Slot 0 name correct");
            Assert(!info[0].HasMissionSnapshot, "Slot 0 has no mission snapshot");

            Assert(!info[1].IsOccupied, "Slot 1 should be empty");

            Assert(info[2].IsOccupied, "Slot 2 should be occupied");
            Assert(info[2].HasMissionSnapshot, "Slot 2 should have mission snapshot");
        }

        // === Restore Point ===

        public void RestorePoint_SaveAndLoad()
        {
            var mgr = MakeManager();
            var save = MakeMissionSave();

            bool saved = mgr.SaveRestorePoint(save);
            Assert(saved, "Restore point save should succeed");
            Assert(mgr.HasRestorePoint(), "Should have restore point");

            var loaded = mgr.LoadRestorePoint();
            Assert(loaded != null, "Should load restore point");
            Assert(loaded.MissionSnapshot.RoundNumber == 4, "Round should round-trip");
            Assert(loaded.MissionSnapshot.Rangers.Count == 1, "Rangers should round-trip");
            Assert(loaded.MissionSnapshot.Enemies.Count == 1, "Enemies should round-trip");
        }

        public void RestorePoint_Overwrites()
        {
            var mgr = MakeManager();

            var save1 = MakeMissionSave();
            save1.MissionSnapshot.RoundNumber = 2;
            mgr.SaveRestorePoint(save1);

            var save2 = MakeMissionSave();
            save2.MissionSnapshot.RoundNumber = 7;
            mgr.SaveRestorePoint(save2);

            var loaded = mgr.LoadRestorePoint();
            Assert(loaded.MissionSnapshot.RoundNumber == 7,
                "Restore point should be overwritten");
        }

        public void RestorePoint_Clear()
        {
            var mgr = MakeManager();
            mgr.SaveRestorePoint(MakeMissionSave());

            Assert(mgr.HasRestorePoint(), "Should exist before clear");

            mgr.ClearRestorePoint();
            Assert(!mgr.HasRestorePoint(), "Should not exist after clear");
        }

        public void RestorePoint_NullSnapshot_ReturnsFalse()
        {
            var mgr = MakeManager();

            Assert(!mgr.SaveRestorePoint(MakeCampaignSave()),
                "Should reject save without mission snapshot");
        }

        public void LoadRestorePoint_WhenEmpty_ReturnsNull()
        {
            var mgr = MakeManager();

            Assert(mgr.LoadRestorePoint() == null,
                "Should return null when no restore point");
        }

        // === Save Data Structure ===

        public void MissionSnapshot_ContainsFullState()
        {
            var save = MakeMissionSave();
            var snap = save.MissionSnapshot;

            Assert(snap.EpisodeId == "episode_6", "Episode ID");
            Assert(snap.RoundNumber == 4, "Round number");
            Assert(snap.LoadoutLocked, "Loadout should be locked");
            Assert(snap.EquippedFormIds.Count == 2, "Equipped forms");

            var ranger = snap.Rangers[0];
            Assert(ranger.MorphState == "Morphed", "Morph state");
            Assert(ranger.CurrentFormId == "form_blaze", "Current form");
            Assert(ranger.FormHealths.Count == 1, "Form health entries");
            Assert(ranger.FormHealths[0].CurrentHealth == 75f, "Form health value");

            var enemy = snap.Enemies[0];
            Assert(enemy.IsAggressive, "Enemy aggression");
            Assert(enemy.CurrentHealth == 15f, "Enemy health");

            Assert(snap.FormPool.Cooldowns.Count == 1, "Form cooldowns");
            Assert(snap.FormPool.Occupancies.Count == 1, "Form occupancies");

            Assert(snap.Scouting.RevealedTypes.Count == 1, "Scouting revealed");
            Assert(snap.Scouting.ObservedEnemyIds.Count == 2, "Scouting observed");
        }

        // === Test Runner ===

        public static void RunAll()
        {
            var t = new SaveManagerTests();

            // Save slots
            t.SaveAndLoad_RoundTrips();
            t.SaveToSlot_SetsTimestamp();
            t.SaveToSlot_OverwritesExisting();
            t.LoadFromSlot_EmptyReturnsNull();
            t.SaveToSlot_InvalidIndex_ReturnsFalse();
            t.SaveToSlot_NullData_ReturnsFalse();
            t.DeleteSlot_RemovesData();
            t.IsSlotOccupied_CorrectState();
            t.MultipleSlots_Independent();

            // Slot info
            t.GetAllSlotInfo_ShowsAllSlots();

            // Restore point
            t.RestorePoint_SaveAndLoad();
            t.RestorePoint_Overwrites();
            t.RestorePoint_Clear();
            t.RestorePoint_NullSnapshot_ReturnsFalse();
            t.LoadRestorePoint_WhenEmpty_ReturnsNull();

            // Data structure
            t.MissionSnapshot_ContainsFullState();

            System.Console.WriteLine("SaveManagerTests: All passed");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new System.Exception($"FAIL: {message}");
        }
    }
}
