using System;

namespace TokuTactics.Core.Health
{
    /// <summary>
    /// Standard health pool implementation. Used by forms, unmorphed state, and zords.
    /// Maximum is set at construction and can be updated (e.g., when DEF changes health pool size).
    /// </summary>
    public class HealthPool : IHealthPool
    {
        public float Current { get; private set; }
        public float Maximum { get; private set; }
        public bool IsAlive => Current > 0;
        public float Percentage => Maximum > 0 ? Current / Maximum : 0f;

        public HealthPool(float maximum)
        {
            Maximum = maximum;
            Current = maximum;
        }

        public float TakeDamage(float amount)
        {
            float actual = Math.Min(amount, Current);
            Current = Math.Max(0, Current - amount);
            return actual;
        }

        public float Heal(float amount)
        {
            float actual = Math.Min(amount, Maximum - Current);
            Current = Math.Min(Maximum, Current + amount);
            return actual;
        }

        public void Regenerate(float amount)
        {
            Current = Math.Min(Maximum, Current + amount);
        }

        public void Reset()
        {
            Current = Maximum;
        }

        /// <summary>
        /// Update the maximum (e.g., when DEF stat changes affect pool size).
        /// Optionally scale current health proportionally.
        /// </summary>
        public void SetMaximum(float newMaximum, bool scaleCurrentProportionally = true)
        {
            if (scaleCurrentProportionally && Maximum > 0)
            {
                float ratio = Current / Maximum;
                Maximum = newMaximum;
                Current = Maximum * ratio;
            }
            else
            {
                Maximum = newMaximum;
                Current = Math.Min(Current, Maximum);
            }
        }
    }
}
