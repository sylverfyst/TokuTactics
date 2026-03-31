using TokuTactics.Core.Health;

namespace TokuTactics.Tests.Core.Health
{
    public class HealthPoolTests
    {
        public void Constructor_StartsAtFullHealth()
        {
            var pool = new HealthPool(100f);

            Assert(pool.Current == 100f, "Should start at max");
            Assert(pool.Maximum == 100f, "Max should be set");
            Assert(pool.IsAlive, "Should be alive");
            Assert(pool.Percentage == 1.0f, "Should be at 100%");
        }

        public void TakeDamage_ReducesCurrent()
        {
            var pool = new HealthPool(100f);

            float actual = pool.TakeDamage(30f);

            Assert(pool.Current == 70f, "Should have 70 health");
            Assert(actual == 30f, "Should report 30 damage dealt");
            Assert(pool.IsAlive, "Should still be alive");
        }

        public void TakeDamage_CapsAtZero()
        {
            var pool = new HealthPool(50f);

            float actual = pool.TakeDamage(80f);

            Assert(pool.Current == 0f, "Should not go below 0");
            Assert(actual == 50f, "Should report only 50 actual damage");
            Assert(!pool.IsAlive, "Should be dead");
        }

        public void TakeDamage_ExactLethal()
        {
            var pool = new HealthPool(100f);

            pool.TakeDamage(100f);

            Assert(pool.Current == 0f, "Should be exactly 0");
            Assert(!pool.IsAlive, "Should be dead at exactly 0");
        }

        public void Heal_RestoresHealth()
        {
            var pool = new HealthPool(100f);
            pool.TakeDamage(40f);

            float actual = pool.Heal(25f);

            Assert(pool.Current == 85f, "Should have 85 health");
            Assert(actual == 25f, "Should report 25 healing");
        }

        public void Heal_CapsAtMaximum()
        {
            var pool = new HealthPool(100f);
            pool.TakeDamage(20f);

            float actual = pool.Heal(50f);

            Assert(pool.Current == 100f, "Should cap at max");
            Assert(actual == 20f, "Should report only 20 actual healing");
        }

        public void Regenerate_RestoresOverTime()
        {
            var pool = new HealthPool(100f);
            pool.TakeDamage(50f);

            pool.Regenerate(10f);
            pool.Regenerate(10f);

            Assert(pool.Current == 70f, "Should have regenerated 20");
        }

        public void Regenerate_CapsAtMaximum()
        {
            var pool = new HealthPool(100f);
            pool.TakeDamage(5f);

            pool.Regenerate(20f);

            Assert(pool.Current == 100f, "Should cap at max");
        }

        public void Reset_RestoresToFull()
        {
            var pool = new HealthPool(100f);
            pool.TakeDamage(75f);

            pool.Reset();

            Assert(pool.Current == 100f, "Should be fully restored");
            Assert(pool.IsAlive, "Should be alive");
        }

        public void SetMaximum_ScalesProportionally()
        {
            var pool = new HealthPool(100f);
            pool.TakeDamage(50f); // 50/100 = 50%

            pool.SetMaximum(200f, scaleCurrentProportionally: true);

            Assert(pool.Maximum == 200f, "Max should be updated");
            Assert(pool.Current == 100f, "Current should scale to 50% of new max");
        }

        public void SetMaximum_WithoutScaling_ClampsToNewMax()
        {
            var pool = new HealthPool(100f);
            // Full health at 100

            pool.SetMaximum(50f, scaleCurrentProportionally: false);

            Assert(pool.Maximum == 50f, "Max should be updated");
            Assert(pool.Current == 50f, "Current should clamp to new max");
        }

        public void Percentage_CalculatesCorrectly()
        {
            var pool = new HealthPool(200f);
            pool.TakeDamage(50f);

            Assert(pool.Percentage == 0.75f, "Should be 75%");
        }

        public void Percentage_ZeroMax_ReturnsZero()
        {
            var pool = new HealthPool(0f);

            Assert(pool.Percentage == 0f, "Zero max should return 0%");
        }

        // === Test Runner ===

        public static void RunAll()
        {
            var tests = new HealthPoolTests();
            tests.Constructor_StartsAtFullHealth();
            tests.TakeDamage_ReducesCurrent();
            tests.TakeDamage_CapsAtZero();
            tests.TakeDamage_ExactLethal();
            tests.Heal_RestoresHealth();
            tests.Heal_CapsAtMaximum();
            tests.Regenerate_RestoresOverTime();
            tests.Regenerate_CapsAtMaximum();
            tests.Reset_RestoresToFull();
            tests.SetMaximum_ScalesProportionally();
            tests.SetMaximum_WithoutScaling_ClampsToNewMax();
            tests.Percentage_CalculatesCorrectly();
            tests.Percentage_ZeroMax_ReturnsZero();
            System.Console.WriteLine("HealthPoolTests: All passed");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new System.Exception($"FAIL: {message}");
        }
    }
}
