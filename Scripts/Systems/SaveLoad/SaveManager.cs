using System;
using System.Collections.Generic;
using System.Linq;

namespace TokuTactics.Systems.SaveLoad
{
    /// <summary>
    /// Manages save slots and mid-mission restore points.
    /// 
    /// The SaveManager works with SaveData objects — it doesn't know how to
    /// convert runtime game state to/from SaveData. That's the game layer's
    /// responsibility (a SaveDataBuilder / SaveDataLoader).
    /// 
    /// Serialization: The manager converts SaveData to/from JSON strings.
    /// The actual file I/O is delegated to an ISaveStorage implementation
    /// so the save system is testable without filesystem access and portable
    /// across platforms (Godot file API, cloud saves, etc.).
    /// 
    /// Save slots: Multiple named slots for between-episode saves.
    /// Restore point: Single overwritable snapshot for mid-mission saves.
    /// </summary>
    public class SaveManager
    {
        private readonly ISaveStorage _storage;
        private readonly ISaveSerializer _serializer;

        /// <summary>Maximum number of save slots.</summary>
        public int MaxSlots { get; set; } = 10;

        public SaveManager(ISaveStorage storage, ISaveSerializer serializer)
        {
            _storage = storage;
            _serializer = serializer;
        }

        // === Save Slots (Between Episodes) ===

        /// <summary>
        /// Save campaign state to a numbered slot.
        /// Returns true if the save succeeded.
        /// </summary>
        public bool SaveToSlot(int slotIndex, SaveData data)
        {
            if (slotIndex < 0 || slotIndex >= MaxSlots) return false;
            if (data == null) return false;

            data.Timestamp = DateTime.UtcNow.ToString("o");
            string key = SlotKey(slotIndex);
            string json = _serializer.Serialize(data);

            return _storage.Write(key, json);
        }

        /// <summary>
        /// Load campaign state from a numbered slot.
        /// Returns null if the slot is empty or corrupted.
        /// </summary>
        public SaveData LoadFromSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= MaxSlots) return null;

            string key = SlotKey(slotIndex);
            string json = _storage.Read(key);

            if (string.IsNullOrEmpty(json)) return null;

            return _serializer.Deserialize(json);
        }

        /// <summary>
        /// Delete a save slot.
        /// </summary>
        public bool DeleteSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= MaxSlots) return false;
            return _storage.Delete(SlotKey(slotIndex));
        }

        /// <summary>
        /// Check if a save slot has data.
        /// </summary>
        public bool IsSlotOccupied(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= MaxSlots) return false;
            return _storage.Exists(SlotKey(slotIndex));
        }

        /// <summary>
        /// Get summary info for all save slots (for the save/load UI).
        /// </summary>
        public List<SaveSlotInfo> GetAllSlotInfo()
        {
            var slots = new List<SaveSlotInfo>();
            for (int i = 0; i < MaxSlots; i++)
            {
                var info = new SaveSlotInfo { SlotIndex = i, IsOccupied = false };

                if (IsSlotOccupied(i))
                {
                    var data = LoadFromSlot(i);
                    if (data != null)
                    {
                        info.IsOccupied = true;
                        info.SlotName = data.SlotName;
                        info.Timestamp = data.Timestamp;
                        info.PlayTimeSeconds = data.PlayTimeSeconds;
                        info.LastEpisodeId = data.Campaign?.LastCompletedEpisodeId;
                        info.HasMissionSnapshot = data.MissionSnapshot != null;
                    }
                }

                slots.Add(info);
            }
            return slots;
        }

        // === Restore Point (Mid-Mission) ===

        private const string RestorePointKey = "restore_point";

        /// <summary>
        /// Save a mid-mission restore point. Overwrites any existing restore point.
        /// The SaveData should include both CampaignData and MissionSnapshot.
        /// </summary>
        public bool SaveRestorePoint(SaveData data)
        {
            if (data == null || data.MissionSnapshot == null) return false;

            data.Timestamp = DateTime.UtcNow.ToString("o");
            string json = _serializer.Serialize(data);

            return _storage.Write(RestorePointKey, json);
        }

        /// <summary>
        /// Load the mid-mission restore point. Returns null if none exists.
        /// </summary>
        public SaveData LoadRestorePoint()
        {
            string json = _storage.Read(RestorePointKey);
            if (string.IsNullOrEmpty(json)) return null;

            return _serializer.Deserialize(json);
        }

        /// <summary>
        /// Check if a restore point exists.
        /// </summary>
        public bool HasRestorePoint()
        {
            return _storage.Exists(RestorePointKey);
        }

        /// <summary>
        /// Clear the restore point (e.g., on mission completion or new mission start).
        /// </summary>
        public bool ClearRestorePoint()
        {
            return _storage.Delete(RestorePointKey);
        }

        // === Internal ===

        private string SlotKey(int index) => $"save_slot_{index}";
    }

    // === Abstractions ===

    /// <summary>
    /// Abstraction for save file I/O.
    /// Implementations handle platform-specific file access:
    /// - MemorySaveStorage: for testing
    /// - GodotSaveStorage: uses Godot's FileAccess API (user:// directory)
    /// - CloudSaveStorage: future cloud save support
    /// </summary>
    public interface ISaveStorage
    {
        bool Write(string key, string data);
        string Read(string key);
        bool Delete(string key);
        bool Exists(string key);
    }

    /// <summary>
    /// Abstraction for save data serialization.
    /// Implementations handle format-specific serialization:
    /// - JsonSaveSerializer: System.Text.Json or Newtonsoft
    /// - GodotJsonSerializer: Godot's built-in JSON
    /// </summary>
    public interface ISaveSerializer
    {
        string Serialize(SaveData data);
        SaveData Deserialize(string json);
    }

    // === In-Memory Implementations (Testing) ===

    /// <summary>
    /// In-memory save storage for testing. No file I/O.
    /// </summary>
    public class MemorySaveStorage : ISaveStorage
    {
        private readonly Dictionary<string, string> _data = new();

        public bool Write(string key, string data)
        {
            _data[key] = data;
            return true;
        }

        public string Read(string key)
        {
            return _data.ContainsKey(key) ? _data[key] : null;
        }

        public bool Delete(string key)
        {
            return _data.Remove(key);
        }

        public bool Exists(string key)
        {
            return _data.ContainsKey(key);
        }

        /// <summary>Number of entries stored (for testing).</summary>
        public int Count => _data.Count;
    }

    /// <summary>
    /// Minimal JSON serializer for testing.
    /// Uses a simple format that doesn't require System.Text.Json or Newtonsoft.
    /// For production, replace with a real JSON library.
    /// 
    /// This implementation stores the serialized object reference directly —
    /// it validates the serialize/deserialize round-trip works but doesn't
    /// produce actual JSON strings. The ISaveSerializer interface ensures
    /// the real implementation can be swapped in.
    /// </summary>
    public class PassthroughSaveSerializer : ISaveSerializer
    {
        private readonly Dictionary<string, SaveData> _roundTrip = new();
        private int _counter;

        public string Serialize(SaveData data)
        {
            string key = $"__save_{_counter++}";
            _roundTrip[key] = data;
            return key;
        }

        public SaveData Deserialize(string json)
        {
            return _roundTrip.ContainsKey(json) ? _roundTrip[json] : null;
        }
    }

    // === UI Data ===

    /// <summary>
    /// Summary info for a save slot, displayed in the save/load UI.
    /// </summary>
    public class SaveSlotInfo
    {
        public int SlotIndex { get; set; }
        public bool IsOccupied { get; set; }
        public string SlotName { get; set; }
        public string Timestamp { get; set; }
        public double PlayTimeSeconds { get; set; }
        public string LastEpisodeId { get; set; }
        public bool HasMissionSnapshot { get; set; }
    }
}
