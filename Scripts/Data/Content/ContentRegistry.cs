using System.Collections.Generic;
using TokuTactics.Entities.Enemies;
using TokuTactics.Entities.Forms;

namespace TokuTactics.Data.Content
{
    /// <summary>
    /// Centralized lookup for all content definitions by ID.
    /// Populated once at game startup from the catalog classes.
    /// Systems use this to resolve string IDs to data objects
    /// (e.g., EnemySpawnEntry.EnemyDataId → EnemyData).
    /// 
    /// This replaces scattered static method calls with a single queryable registry.
    /// Adding new content = registering it here.
    /// </summary>
    public class ContentRegistry
    {
        private readonly Dictionary<string, EnemyData> _enemies = new();
        private readonly Dictionary<string, FormData> _forms = new();
        private readonly Dictionary<string, MapDefinition> _maps = new();
        private readonly Dictionary<string, EpisodeDefinition> _episodes = new();
        private readonly Dictionary<string, RangerDefinition> _rangers = new();

        // === Registration ===

        public void RegisterEnemy(EnemyData data) => _enemies[data.Id] = data;
        public void RegisterForm(FormData data) => _forms[data.Id] = data;
        public void RegisterMap(MapDefinition data) => _maps[data.Id] = data;
        public void RegisterEpisode(EpisodeDefinition data) => _episodes[data.Id] = data;
        public void RegisterRanger(RangerDefinition data) => _rangers[data.Id] = data;

        // === Lookup ===

        public EnemyData GetEnemy(string id) =>
            _enemies.ContainsKey(id) ? _enemies[id] : null;

        public FormData GetForm(string id) =>
            _forms.ContainsKey(id) ? _forms[id] : null;

        public MapDefinition GetMap(string id) =>
            _maps.ContainsKey(id) ? _maps[id] : null;

        public EpisodeDefinition GetEpisode(string id) =>
            _episodes.ContainsKey(id) ? _episodes[id] : null;

        public RangerDefinition GetRanger(string id) =>
            _rangers.ContainsKey(id) ? _rangers[id] : null;

        // === Queries ===

        public IReadOnlyDictionary<string, EnemyData> AllEnemies => _enemies;
        public IReadOnlyDictionary<string, FormData> AllForms => _forms;
        public IReadOnlyDictionary<string, MapDefinition> AllMaps => _maps;
        public IReadOnlyDictionary<string, EpisodeDefinition> AllEpisodes => _episodes;
        public IReadOnlyDictionary<string, RangerDefinition> AllRangers => _rangers;

        // === Vertical Slice Setup ===

        /// <summary>
        /// Register all vertical slice content. Call once at game startup.
        /// New episodes just add content here — no new systems needed.
        /// </summary>
        public static ContentRegistry CreateVerticalSlice()
        {
            var reg = new ContentRegistry();

            // Forms
            reg.RegisterForm(FormCatalog.BaseForm());
            reg.RegisterForm(FormCatalog.BlazeForm());
            reg.RegisterForm(FormCatalog.TorrentForm());
            reg.RegisterForm(FormCatalog.FrostForm());

            // Enemies
            reg.RegisterEnemy(EnemyCatalog.Putty());
            reg.RegisterEnemy(EnemyCatalog.BlazeGrunt());
            reg.RegisterEnemy(EnemyCatalog.FrostWyrm());
            reg.RegisterEnemy(EnemyCatalog.ShadowCommander());

            // Rangers
            foreach (var rangerDef in RangerCatalog.AllRangers())
                reg.RegisterRanger(rangerDef);

            // Maps
            reg.RegisterMap(MapCatalog.FrozenOutpost());

            // Episodes
            reg.RegisterEpisode(EpisodeCatalog.FrozenOutpostEpisode());

            return reg;
        }
    }
}
