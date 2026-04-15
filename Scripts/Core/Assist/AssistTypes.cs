using System.Collections.Generic;
using TokuTactics.Core.Types;

namespace TokuTactics.Core.Assist
{
    /// <summary>
    /// State of a potential assister, provided by the combat system.
    /// </summary>
    public class AssistCandidateState
    {
        public bool IsMorphed { get; set; }
        public string CurrentFormId { get; set; }
        public string BaseFormId { get; set; }
        public bool IsInBaseForm { get; set; }
        public float WeaponBasePower { get; set; }
        public float Str { get; set; }
        public float Cha { get; set; }
        public DualType AssisterDualType { get; set; }
        public bool HasUsedBondRefresh { get; set; }
        public bool HasReceivedBondRefresh { get; set; }
    }

    /// <summary>
    /// Complete resolution of all assists for a single attack action.
    /// </summary>
    public class AssistResolution
    {
        public List<AssistEffect> Assists { get; } = new();
        public bool HasAssists => Assists.Count > 0;
        public bool HasPairAttacks => Assists.Exists(a => a.IsPairAttack);
        public bool HasFormDisruptions => Assists.Exists(a => a.ForceToBaseForm);
        public bool HasRefreshOpportunities => Assists.Exists(a => a.CanRefreshPartner);
        public static AssistResolution Empty => new AssistResolution();
    }

    /// <summary>
    /// A single assist from one adjacent Ranger.
    /// </summary>
    public class AssistEffect
    {
        public string AssisterId { get; set; }
        public string AttackerId { get; set; }
        public int BondTier { get; set; }
        public string AssisterFormId { get; set; }
        public float AssisterWeaponPower { get; set; }
        public float AssisterStr { get; set; }
        public float ChaMultiplier { get; set; }
        public DualType AssisterDualType { get; set; }
        public float DamageMultiplier { get; set; }
        public bool IsPairAttack { get; set; }
        public bool ForceToBaseForm { get; set; }
        public string VacatedFormId { get; set; }
        public bool CanRefreshPartner { get; set; }
    }
}
