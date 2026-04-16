using System;
using System.Collections.Generic;
using TokuTactics.Commands.AI;
using TokuTactics.Core.Grid;

namespace TokuTactics.Tests.Commands.AI
{
    public static class ResolveEnemyTurnTests
    {
        public static void Run()
        {
            Test_InRange_AttackOnly();
            Test_OutOfRange_MoveAndAttack();
            Test_OutOfRange_MoveOnly();
            Test_NoRangers_Nothing();
            Test_UsesInjectedBricks();
            Console.WriteLine("ResolveEnemyTurnTests: All passed");
        }

        private static void Test_InRange_AttackOnly()
        {
            var grid = new BattleGrid(10, 10);
            grid.PlaceUnit("enemy1", new GridPosition(5, 5));
            grid.PlaceUnit("r1", new GridPosition(5, 4));
            var rangers = new HashSet<string> { "r1" };

            var result = ResolveEnemyTurn.Execute(grid, "enemy1", 3, 1, rangers);

            Assert(!result.DidNothing, "Should do something");
            Assert(result.MoveDestination == null, "Should not move — already in range");
            Assert(result.AttackTargetId == "r1", $"Should attack r1, got {result.AttackTargetId}");
        }

        private static void Test_OutOfRange_MoveAndAttack()
        {
            var grid = new BattleGrid(10, 10);
            grid.PlaceUnit("enemy1", new GridPosition(5, 5));
            grid.PlaceUnit("r1", new GridPosition(5, 2));
            var rangers = new HashSet<string> { "r1" };

            var result = ResolveEnemyTurn.Execute(grid, "enemy1", 3, 1, rangers);

            Assert(!result.DidNothing, "Should do something");
            Assert(result.MoveDestination.HasValue, "Should move");
            // After moving 3 tiles toward (5,2) from (5,5), should reach (5,3) — distance 1 from target
            Assert(result.AttackTargetId == "r1", "Should be able to attack after moving");
        }

        private static void Test_OutOfRange_MoveOnly()
        {
            var grid = new BattleGrid(10, 10);
            grid.PlaceUnit("enemy1", new GridPosition(0, 0));
            grid.PlaceUnit("r1", new GridPosition(9, 9));
            var rangers = new HashSet<string> { "r1" };

            var result = ResolveEnemyTurn.Execute(grid, "enemy1", 3, 1, rangers);

            Assert(!result.DidNothing, "Should move toward target");
            Assert(result.MoveDestination.HasValue, "Should have a move destination");
            Assert(result.AttackTargetId == null, "Should not be in attack range after moving 3 tiles");
        }

        private static void Test_NoRangers_Nothing()
        {
            var grid = new BattleGrid(10, 10);
            grid.PlaceUnit("enemy1", new GridPosition(5, 5));

            var result = ResolveEnemyTurn.Execute(grid, "enemy1", 3, 1, new HashSet<string>());

            Assert(result.DidNothing, "No rangers should mean nothing to do");
        }

        private static void Test_UsesInjectedBricks()
        {
            var grid = new BattleGrid(10, 10);
            grid.PlaceUnit("enemy1", new GridPosition(5, 5));
            bool findNearestCalled = false;

            ResolveEnemyTurn.Execute(grid, "enemy1", 3, 1, new HashSet<string> { "r1" },
                findNearest: (g, p, t) =>
                {
                    findNearestCalled = true;
                    return null;
                });

            Assert(findNearestCalled, "Should call injected findNearest");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception($"FAIL: {message}");
        }
    }
}
