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

            // 공격력 0 (테스트 더미 등) 은 피해 0. 그 외에는 최소 1.
            int amount = attacker.Stats.ATK * atkMul * coef <= 0f ? 0 : Mathf.Max(1, Mathf.RoundToInt(raw));
            return new HitResult { Amount = amount, Crit = crit, Guarded = guarded };
        }

        /// <summary>
        /// v0.2 스킬 피해 (스킬 문서 4장): 물리 = 공격력 × 계수 × (1 − 방어력/100),
        /// 마법 = (기본 피해량 + 마력 × 계수) × (1 − 저항력/100). 합에 배율 → 치명타 → ±난수 → 방어 감소.
        /// 배율이 0 이면 (수치 미정 캐릭터) 피해 0. `[해석]` 치명타 · 난수는 합 전체에 적용.
        /// </summary>
        /// <param name="resDown">대상 저항력 감소 (어둠 잠식 1당 -1 등)</param>
        public static HitResult ComputeSkill(Combatant attacker, Combatant target, float atkCoef, float magicBase,
                                             float magicCoef, System.Random rng, float mul = 1f, int resDown = 0)
        {
            float phys = attacker.Stats.ATK * atkCoef * (1f - Mathf.Clamp(target.Stats.DEF, 0, 100) / 100f);
            float magic = (magicBase + attacker.Stats.MAG * magicCoef) * (1f - Mathf.Clamp(target.Stats.RES - resDown, 0, 100) / 100f);
            float raw = (phys + magic) * mul;
            if (raw <= 0f) return new HitResult { Amount = 0 };

            bool crit = rng.NextDouble() < Mathf.Clamp01(attacker.Stats.CRIT);
            if (crit) raw *= attacker.Stats.CRITDMG;
            raw *= 1f + (float)(rng.NextDouble() * 2.0 - 1.0) * GameData.DamageVariance;

            bool guarded = target.Guarding;
            if (guarded) raw *= GameData.GuardReduction;

            return new HitResult { Amount = Mathf.Max(1, Mathf.RoundToInt(raw)), Crit = crit, Guarded = guarded };
        }

        /// <summary>고정 피해 = 대상 최대 체력 × 비율 × 배율 (방어 · 저항 · 치명 무시). 배율 0 이면 0.</summary>
        public static int Fixed(Combatant target, float ratioOfMaxHp, float mul = 1f)
        {
            float raw = target.Stats.HP * ratioOfMaxHp * mul;
            return raw <= 0f ? 0 : Mathf.Max(1, Mathf.RoundToInt(raw));
        }

        /// <summary>회복량 = ATK × 버프 × 계수 (v0.2). 피해와 같은 파일에서만 계산한다.</summary>
        public static int Heal(Combatant caster, float coef, float atkMul = 1f)
        {
            return Mathf.Max(1, Mathf.RoundToInt(caster.Stats.ATK * atkMul * coef));
        }
    }
}
