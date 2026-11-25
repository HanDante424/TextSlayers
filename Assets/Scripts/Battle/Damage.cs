using UnityEngine;

namespace TextSlayers
{
    public struct HitResult
    {
        public int Amount;
        public bool Crit;
        public bool Guarded;
    }

    /// <summary>
    /// 피해 계산은 이 파일 한 곳에서만 한다 (기획 v0.1 5장).
    /// 피해 = ATK × 계수 × DefBase / (DefBase + DEF) → 치명타 × CRITDMG → ±난수 → 방어 감소 → 최소 1
    /// </summary>
    public static class Damage
    {
        /// <param name="atkMul">공격력 배율 (버프). v0.1 전투는 기본값 1.</param>
        public static HitResult Compute(Combatant attacker, Combatant target, float coef, float bonusCrit, System.Random rng, float atkMul = 1f)
        {
            float raw = attacker.Stats.ATK * atkMul * coef * GameData.DefBase / (GameData.DefBase + Mathf.Max(0, target.Stats.DEF));

            bool crit = rng.NextDouble() < Mathf.Clamp01(attacker.Stats.CRIT + bonusCrit);
            if (crit) raw *= attacker.Stats.CRITDMG;

            raw *= 1f + (float)(rng.NextDouble() * 2.0 - 1.0) * GameData.DamageVariance;

            bool guarded = target.Guarding;
            if (guarded) raw *= GameData.GuardReduction;

            return new HitResult { Amount = Mathf.Max(1, Mathf.RoundToInt(raw)), Crit = crit, Guarded = guarded };
        }

        /// <summary>회복량 = ATK × 버프 × 계수 (v0.2). 피해와 같은 파일에서만 계산한다.</summary>
        public static int Heal(Combatant caster, float coef, float atkMul = 1f)
        {
            return Mathf.Max(1, Mathf.RoundToInt(caster.Stats.ATK * atkMul * coef));
        }
    }
}
