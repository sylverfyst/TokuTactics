using System.Linq;
using TokuTactics.Core.Grid;
using TokuTactics.Core.Types;
using TokuTactics.Core.Stats;
using TokuTactics.Entities.Enemies;
using TokuTactics.Entities.Forms;
using TokuTactics.Entities.Rangers;
using TokuTactics.Data.Content;
using TokuTactics.Data.Content.PersonalAbilities;

namespace TokuTactics.Tests.Data
{
    public class ContentTests
    {
        // === Type Chart ===

        public void TypeChart_BlazeBeatsForst()
        {
            var chart = TypeChartSetup.Create();

            Assert(chart.CheckSingle(ElementalType.Blaze, ElementalType.Frost) == 1,
                "Blaze should be strong against Frost");
        }

        public void TypeChart_FrostBeatsTorrent()
        {
            var chart = TypeChartSetup.Create();

            Assert(chart.CheckSingle(ElementalType.Frost, ElementalType.Torrent) == 1,
                "Frost should be strong against Torrent");
        }

        public void TypeChart_TorrentBeatsBlaze()
        {
            var chart = TypeChartSetup.Create();

            Assert(chart.CheckSingle(ElementalType.Torrent, ElementalType.Blaze) == 1,
                "Torrent should be strong against Blaze");
        }

        public void TypeChart_NormalIsNeutralAgainstEverything()
        {
            var chart = TypeChartSetup.Create();

            foreach (ElementalType type in System.Enum.GetValues(typeof(ElementalType)))
            {
                Assert(chart.CheckSingle(ElementalType.Normal, type) == 0,
                    $"Normal should be neutral against {type}");
                Assert(chart.CheckSingle(type, ElementalType.Normal) == 0,
                    $"{type} should be neutral against Normal");
            }
        }

        // === Forms ===

        public void Forms_BaseForm_IsNormalType()
        {
            var form = FormCatalog.BaseForm();

            Assert(form.Type == ElementalType.Normal, "Base form should be Normal type");
            Assert(form.BasicAttackRange == 1, "Base form should be melee");
        }

        public void Forms_BlazeForm_IsMelee()
        {
            var form = FormCatalog.BlazeForm();

            Assert(form.Type == ElementalType.Blaze, "Should be Blaze type");
            Assert(form.BasicAttackRange == 1, "Should be melee range");
            Assert(form.BaseStats.Get(StatType.STR) > form.BaseStats.Get(StatType.MAG),
                "Blaze should have higher STR than MAG");
        }

        public void Forms_TorrentForm_IsRanged()
        {
            var form = FormCatalog.TorrentForm();

            Assert(form.Type == ElementalType.Torrent, "Should be Torrent type");
            Assert(form.BasicAttackRange >= 3, "Should be ranged");
            Assert(form.MovementRange >= 4, "Ranged form should have good mobility");
        }

        public void Forms_FrostForm_IsControlCaster()
        {
            var form = FormCatalog.FrostForm();

            Assert(form.Type == ElementalType.Frost, "Should be Frost type");
            Assert(form.BaseStats.Get(StatType.MAG) > form.BaseStats.Get(StatType.STR),
                "Frost should have higher MAG than STR");
            Assert(form.CooldownDuration > FormCatalog.BlazeForm().CooldownDuration,
                "Control form should have longer cooldown (more valuable)");
        }

        public void Forms_AllHaveWeapons()
        {
            var forms = new[] { FormCatalog.BaseForm(), FormCatalog.BlazeForm(),
                FormCatalog.TorrentForm(), FormCatalog.FrostForm() };

            foreach (var form in forms)
            {
                Assert(form.WeaponA != null, $"{form.Name} should have weapon A");
                Assert(form.WeaponB != null, $"{form.Name} should have weapon B");
                Assert(form.WeaponA.Id != form.WeaponB.Id,
                    $"{form.Name} weapons should have different IDs");
            }
        }

        public void Forms_FrostWeaponA_HasStun()
        {
            var weapon = FormCatalog.FrostWeaponA();

            Assert(weapon.StatusEffect != null, "Ice Lance should have a status effect");
            Assert(weapon.StatusEffect.EffectId == "eff_stun", "Should be stun");
        }

        public void Forms_BlazeWeaponB_HasBurn()
        {
            var weapon = FormCatalog.BlazeWeaponB();

            Assert(weapon.StatusEffect != null, "Flame Sword should have a status effect");
            Assert(weapon.StatusEffect.EffectId == "eff_burn", "Should be burn");
        }

        // === Enemies ===

        public void Enemies_Putty_IsTypeless()
        {
            var putty = EnemyCatalog.Putty();

            Assert(putty.Type == null, "Putty should be typeless");
            Assert(putty.Tier == EnemyTier.FootSoldier, "Should be foot soldier");
            Assert(putty.Weapon == null, "Foot soldiers have no weapon");
            Assert(putty.Gimmick == null, "Foot soldiers have no gimmick");
        }

        public void Enemies_BlazeGrunt_IsTyped()
        {
            var grunt = EnemyCatalog.BlazeGrunt();

            Assert(grunt.Type == ElementalType.Blaze, "Should be Blaze type");
            Assert(grunt.Tier == EnemyTier.FootSoldier, "Should be foot soldier");
        }

        public void Enemies_FrostWyrm_HasTerrainGimmick()
        {
            var wyrm = EnemyCatalog.FrostWyrm();

            Assert(wyrm.Tier == EnemyTier.Monster, "Should be Monster tier");
            Assert(wyrm.Type == ElementalType.Frost, "Should be Frost type");
            Assert(wyrm.Gimmick != null, "Monster should have gimmick");
            Assert(wyrm.Gimmick.Trigger.IsVoluntary, "Terrain gimmick should be voluntary");
            Assert(wyrm.Weapon == null, "Monsters don't have weapons");
        }

        public void Enemies_ShadowCommander_HasWeaponAndGimmick()
        {
            var commander = EnemyCatalog.ShadowCommander();

            Assert(commander.Tier == EnemyTier.Lieutenant, "Should be Lieutenant tier");
            Assert(commander.Type == ElementalType.Shadow, "Should be Shadow type");
            Assert(commander.Weapon != null, "Lieutenant should have weapon");
            Assert(commander.Gimmick != null, "Lieutenant should have gimmick");
            Assert(commander.UsesUtilityScoring, "Lieutenant should use utility scoring");
            Assert(commander.Weapon.StatusEffect != null, "Dark Blade should have bleed");
        }

        public void Enemies_ConstructAsInstances()
        {
            // Verify all enemies can be constructed as runtime instances
            var putty = new Enemy("putty_1", EnemyCatalog.Putty());
            var grunt = new Enemy("grunt_1", EnemyCatalog.BlazeGrunt());
            var wyrm = new Enemy("wyrm_1", EnemyCatalog.FrostWyrm());
            var commander = new Enemy("commander_1", EnemyCatalog.ShadowCommander());

            Assert(putty.IsAlive, "Putty should be alive");
            Assert(grunt.IsAlive, "Grunt should be alive");
            Assert(wyrm.IsAlive, "Wyrm should be alive");
            Assert(commander.IsAlive, "Commander should be alive");
            Assert(commander.HasWeapon && commander.HasGimmick,
                "Commander should have both weapon and gimmick");
        }

        // === Rangers ===

        public void Rangers_AllFiveExist()
        {
            var rangers = RangerCatalog.AllRangers();

            Assert(rangers.Length == 5, "Should have 5 Rangers");
        }

        public void Rangers_UniqueTypes()
        {
            var rangers = RangerCatalog.AllRangers();
            var types = rangers.Select(r => r.IntrinsicType).Distinct().Count();

            Assert(types == 5, "All 5 Rangers should have unique types");
        }

        public void Rangers_AllHaveAbilities()
        {
            var rangers = RangerCatalog.AllRangers();

            foreach (var ranger in rangers)
            {
                Assert(ranger.PersonalAbility != null,
                    $"{ranger.Name} should have a personal ability");
            }
        }

        public void Rangers_AllHaveBaseFormData()
        {
            var rangers = RangerCatalog.AllRangers();

            foreach (var ranger in rangers)
            {
                Assert(ranger.BaseFormData != null,
                    $"{ranger.Name} should have base form data");
                Assert(ranger.BaseFormData.Id == "form_base",
                    $"{ranger.Name} base form should be form_base");
            }
        }

        // === Personal Abilities ===

        public void ScoutPush_CanExecute_Adjacent()
        {
            var push = new ScoutPush();
            var context = new AbilityContext
            {
                Target = "enemy",
                SourcePosition = new GridPosition(5, 5),
                TargetPosition = new GridPosition(5, 4),
                SourceMag = 4f
            };

            Assert(push.CanExecute(context), "Should be executable when adjacent");
        }

        public void ScoutPush_CannotExecute_TooFar()
        {
            var push = new ScoutPush();
            var context = new AbilityContext
            {
                Target = "enemy",
                SourcePosition = new GridPosition(5, 5),
                TargetPosition = new GridPosition(5, 3) // Distance 2, range is 1
            };

            Assert(!push.CanExecute(context), "Should not be executable when too far");
        }

        public void ScoutPush_CannotExecute_NoTarget()
        {
            var push = new ScoutPush();
            var context = new AbilityContext
            {
                Target = null,
                SourcePosition = new GridPosition(5, 5),
                TargetPosition = new GridPosition(5, 4)
            };

            Assert(!push.CanExecute(context), "Should not be executable without target");
        }

        public void ScoutPush_Output_BasePush()
        {
            var push = new ScoutPush();
            var context = new AbilityContext
            {
                Target = "enemy",
                SourcePosition = new GridPosition(5, 5),
                TargetPosition = new GridPosition(5, 4),
                SourceMag = 4f // Below threshold
            };

            var output = push.GetOutput(context);

            Assert(output.HasEffect, "Should have effect");
            Assert(output.DisplaceTargetDistance == 2, "Base push distance should be 2");
            Assert(output.DisplaceTargetPush, "Should be a push (away)");
        }

        public void ScoutPush_Output_MagScaling()
        {
            var push = new ScoutPush();
            var context = new AbilityContext
            {
                Target = "enemy",
                SourcePosition = new GridPosition(5, 5),
                TargetPosition = new GridPosition(5, 4),
                SourceMag = 12f // Above threshold
            };

            var output = push.GetOutput(context);

            Assert(output.DisplaceTargetDistance == 3,
                "High MAG should grant +1 push distance (2 + 1 = 3)");
        }

        public void Rally_CanExecute_WithAdjacentAllies()
        {
            var rally = new Rally();
            var context = new AbilityContext
            {
                AdjacentAllyIds = new System.Collections.Generic.List<string> { "ally_1" }
            };

            Assert(rally.CanExecute(context), "Should be executable with adjacent allies");
        }

        public void Rally_CannotExecute_NoAdjacentAllies()
        {
            var rally = new Rally();
            var context = new AbilityContext
            {
                AdjacentAllyIds = new System.Collections.Generic.List<string>()
            };

            Assert(!rally.CanExecute(context), "Should not be executable without adjacent allies");
        }

        public void Rally_Output_ProducesDefBuff()
        {
            var rally = new Rally();
            var context = new AbilityContext
            {
                SourceMag = 5f,
                AdjacentAllyIds = new System.Collections.Generic.List<string> { "ally_1" }
            };

            var output = rally.GetOutput(context);

            Assert(output.HasEffect, "Should have effect");
            Assert(output.StatusEffect != null, "Should produce a status effect");
            Assert(output.StatusEffect.EffectId == "eff_rally_def", "Should be rally DEF buff");
            Assert(output.StatusEffect.BaseDuration == 2, "Should last 2 turns");
        }

        // === Map ===

        public void Map_BuildsValidGrid()
        {
            var map = MapCatalog.FrozenOutpost();
            var grid = map.BuildGrid();

            Assert(grid.Width == 12, "Map should be 12 wide");
            Assert(grid.Height == 10, "Map should be 10 tall");
        }

        public void Map_HasCorrectTerrainVariety()
        {
            var map = MapCatalog.FrozenOutpost();
            var grid = map.BuildGrid();

            Assert(grid.GetTilesOfType(TerrainType.Wall).Count > 0, "Should have walls");
            Assert(grid.GetTilesOfType(TerrainType.Rough).Count > 0, "Should have rough terrain");
            Assert(grid.GetTilesOfType(TerrainType.Cover).Count > 0, "Should have cover");
            Assert(grid.GetTilesOfType(TerrainType.Hazard).Count > 0, "Should have hazards");
            Assert(grid.GetTilesOfType(TerrainType.Destructible).Count > 0,
                "Should have destructibles");
            Assert(grid.GetTilesOfType(TerrainType.Gap).Count > 0, "Should have gaps");
            Assert(grid.GetTilesOfType(TerrainType.HighGround).Count > 0,
                "Should have high ground");
        }

        public void Map_HasHighGround()
        {
            var map = MapCatalog.FrozenOutpost();
            var grid = map.BuildGrid();

            var tile = grid.GetTile(new GridPosition(5, 1));
            Assert(tile.Terrain == TerrainType.HighGround,
                "High ground tiles should have HighGround terrain type");
            Assert(tile.Elevation > 0, "High ground tiles should have elevation > 0");
        }

        public void Map_SpawnPositionsAreValid()
        {
            var map = MapCatalog.FrozenOutpost();
            var grid = map.BuildGrid();

            Assert(map.RangerSpawns.Count >= 5, "Should have at least 5 Ranger spawns");

            foreach (var pos in map.RangerSpawns)
            {
                Assert(grid.IsTilePassable(pos),
                    $"Ranger spawn at {pos} should be on passable terrain");
            }
        }

        public void Map_RangerSpawnPositionsAreUnique()
        {
            var map = MapCatalog.FrozenOutpost();
            var unique = map.RangerSpawns.Distinct().Count();
            Assert(unique == map.RangerSpawns.Count,
                "All Ranger spawn positions should be unique");
        }

        // === Episode ===

        public void Episode_HasEnemySpawns()
        {
            var episode = EpisodeCatalog.FrozenOutpostEpisode();
            var groundPhase = episode.Phases[0];

            Assert(groundPhase.EnemySpawns.Count == 7,
                "Should have 7 enemy spawns");
        }

        public void Episode_SpawnPositionsArePassable()
        {
            var episode = EpisodeCatalog.FrozenOutpostEpisode();
            var registry = ContentRegistry.CreateVerticalSlice();
            var map = registry.GetMap(episode.MapId);
            var grid = map.BuildGrid();

            var groundPhase = episode.Phases[0];
            foreach (var spawn in groundPhase.EnemySpawns)
            {
                Assert(!grid.IsTileBlocking(spawn.Position),
                    $"Enemy spawn {spawn.InstanceId} at {spawn.Position} should not be on blocking terrain");
            }
        }

        public void Episode_SpawnPositionsAreUnique()
        {
            var episode = EpisodeCatalog.FrozenOutpostEpisode();
            var map = MapCatalog.FrozenOutpost();

            var allPositions = map.RangerSpawns.ToList();
            var groundPhase = episode.Phases[0];
            allPositions.AddRange(groundPhase.EnemySpawns.Select(s => s.Position));

            var unique = allPositions.Distinct().Count();
            Assert(unique == allPositions.Count,
                "All spawn positions should be unique across Rangers and enemies");
        }

        public void Episode_DefeatTargetsExist()
        {
            var episode = EpisodeCatalog.FrozenOutpostEpisode();
            var groundPhase = episode.Phases[0];
            var spawnIds = groundPhase.EnemySpawns.Select(s => s.InstanceId).ToHashSet();

            foreach (var targetId in episode.DefeatTargetIds)
            {
                Assert(spawnIds.Contains(targetId),
                    $"Defeat target '{targetId}' should exist in enemy spawns");
            }
        }

        public void Episode_EnemyDataIdsResolve()
        {
            var episode = EpisodeCatalog.FrozenOutpostEpisode();
            var registry = ContentRegistry.CreateVerticalSlice();
            var groundPhase = episode.Phases[0];

            foreach (var spawn in groundPhase.EnemySpawns)
            {
                Assert(registry.GetEnemy(spawn.EnemyDataId) != null,
                    $"Enemy data '{spawn.EnemyDataId}' should exist in registry");
            }
        }

        // === Content Registry ===

        public void ContentRegistry_AllFormsRegistered()
        {
            var registry = ContentRegistry.CreateVerticalSlice();

            Assert(registry.GetForm("form_base") != null, "Base form registered");
            Assert(registry.GetForm("form_blaze") != null, "Blaze form registered");
            Assert(registry.GetForm("form_torrent") != null, "Torrent form registered");
            Assert(registry.GetForm("form_frost") != null, "Frost form registered");
        }

        public void ContentRegistry_AllEnemiesRegistered()
        {
            var registry = ContentRegistry.CreateVerticalSlice();

            Assert(registry.GetEnemy("foot_putty") != null, "Putty registered");
            Assert(registry.GetEnemy("foot_blaze_grunt") != null, "Blaze Grunt registered");
            Assert(registry.GetEnemy("monster_frost_wyrm") != null, "Frost Wyrm registered");
            Assert(registry.GetEnemy("lt_shadow_commander") != null, "Shadow Commander registered");
        }

        // === Zords ===

        public void Zords_AllFiveExist()
        {
            var zords = ZordCatalog.AllBaseZords();

            Assert(zords.Length == 5, "Should have 5 base zords");
        }

        public void Zords_MatchRangerTypes()
        {
            // Base zords should match their Ranger's innate type
            Assert(ZordCatalog.RedZord().Type == ElementalType.Blaze, "Red zord = Blaze");
            Assert(ZordCatalog.BlueZord().Type == ElementalType.Torrent, "Blue zord = Torrent");
            Assert(ZordCatalog.YellowZord().Type == ElementalType.Volt, "Yellow zord = Volt");
            Assert(ZordCatalog.GreenZord().Type == ElementalType.Gale, "Green zord = Gale");
            Assert(ZordCatalog.PinkZord().Type == ElementalType.Frost, "Pink zord = Frost");
        }

        // === Test Runner ===

        public static void RunAll()
        {
            var t = new ContentTests();

            // Type Chart
            t.TypeChart_BlazeBeatsForst();
            t.TypeChart_FrostBeatsTorrent();
            t.TypeChart_TorrentBeatsBlaze();
            t.TypeChart_NormalIsNeutralAgainstEverything();

            // Forms
            t.Forms_BaseForm_IsNormalType();
            t.Forms_BlazeForm_IsMelee();
            t.Forms_TorrentForm_IsRanged();
            t.Forms_FrostForm_IsControlCaster();
            t.Forms_AllHaveWeapons();
            t.Forms_FrostWeaponA_HasStun();
            t.Forms_BlazeWeaponB_HasBurn();

            // Enemies
            t.Enemies_Putty_IsTypeless();
            t.Enemies_BlazeGrunt_IsTyped();
            t.Enemies_FrostWyrm_HasTerrainGimmick();
            t.Enemies_ShadowCommander_HasWeaponAndGimmick();
            t.Enemies_ConstructAsInstances();

            // Rangers
            t.Rangers_AllFiveExist();
            t.Rangers_UniqueTypes();
            t.Rangers_AllHaveAbilities();
            t.Rangers_AllHaveBaseFormData();

            // Personal Abilities
            t.ScoutPush_CanExecute_Adjacent();
            t.ScoutPush_CannotExecute_TooFar();
            t.ScoutPush_CannotExecute_NoTarget();
            t.ScoutPush_Output_BasePush();
            t.ScoutPush_Output_MagScaling();
            t.Rally_CanExecute_WithAdjacentAllies();
            t.Rally_CannotExecute_NoAdjacentAllies();
            t.Rally_Output_ProducesDefBuff();

            // Map
            t.Map_BuildsValidGrid();
            t.Map_HasCorrectTerrainVariety();
            t.Map_HasHighGround();
            t.Map_SpawnPositionsAreValid();
            t.Map_RangerSpawnPositionsAreUnique();

            // Episode
            t.Episode_HasEnemySpawns();
            t.Episode_SpawnPositionsArePassable();
            t.Episode_SpawnPositionsAreUnique();
            t.Episode_DefeatTargetsExist();
            t.Episode_EnemyDataIdsResolve();

            // Content Registry
            t.ContentRegistry_AllFormsRegistered();
            t.ContentRegistry_AllEnemiesRegistered();

            // Zords
            t.Zords_AllFiveExist();
            t.Zords_MatchRangerTypes();

            System.Console.WriteLine("ContentTests: All passed");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new System.Exception($"FAIL: {message}");
        }
    }
}
