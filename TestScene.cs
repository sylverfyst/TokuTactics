using Godot;
using System;
using TokuTactics.Data.Content;
using TokuTactics.Systems.MissionSetup;
using TokuTactics.Systems.SaveLoad;

public partial class TestScene : Node
{
    public override void _Ready()
    {
        GD.Print("=== Toku Tactics Test Scene ===");
        GD.Print("");

        // Skip test suite for now - it's too verbose for Godot console
        // Uncomment to run all 569 tests:
        // TestRunner.RunAll();

        GD.Print("");

        // Test 2: Initialize ContentRegistry
        GD.Print("Initializing ContentRegistry...");
        try
        {
            var registry = ContentRegistry.CreateVerticalSlice();
            GD.Print($"✓ ContentRegistry initialized");
            GD.Print($"  - Forms: {registry.AllForms.Count}");
            GD.Print($"  - Rangers: {registry.AllRangers.Count}");
            GD.Print($"  - Enemies: {registry.AllEnemies.Count}");
            GD.Print($"  - Maps: {registry.AllMaps.Count}");
            GD.Print($"  - Episodes: {registry.AllEpisodes.Count}");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"✗ ContentRegistry error: {ex.Message}");
        }

        GD.Print("");

        // Test 3: Create MissionContext
        GD.Print("Creating MissionContext for Frozen Outpost episode...");
        try
        {
            var registry = ContentRegistry.CreateVerticalSlice();
            var episode = registry.GetEpisode("episode_frozen_outpost");
            var campaignData = new CampaignData();
            var ctx = MissionContext.Create(episode, campaignData, registry);

            GD.Print($"✓ MissionContext created successfully");
            GD.Print($"  - Episode: {episode.Title}");
            GD.Print($"  - Map size: {ctx.Grid.Width}x{ctx.Grid.Height}");
            GD.Print($"  - Rangers: {ctx.Rangers.Count}");
            GD.Print($"  - Enemies: {ctx.Enemies.Count}");

            // Start mission
            ctx.StartMission();
            GD.Print($"  - Mission started");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"✗ MissionContext error: {ex.Message}");
            GD.PrintErr($"  Stack trace: {ex.StackTrace}");
        }

        GD.Print("");
        GD.Print("=== Test Scene Complete ===");
    }
}
