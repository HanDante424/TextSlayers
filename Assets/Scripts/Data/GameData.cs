using System.Collections.Generic;
using UnityEngine;

namespace TextSlayers
{
    public enum ActionKind { Attack, Guard }

    public sealed class SkillDef
    {
        public string Id;
        public string Name;
        public string Key;      // 화면 표시용 단축키
        public int MpCost;
        public ActionKind Kind;
        public float Coef;      // ATK 배율 (1타당)
        public int Hits = 1;
        public float BonusCrit; // 치명타 확률 가산
        public int MpGain;      // 방어 시 MP 회복
        public string Desc;
    }

    public sealed class EnemyDef
    {
        public string Id;
        public string Name;
        public Stats Stats;
        public int Exp;
        public int Gold;
        public bool IsBoss;
        public string AppearText;
    }

    public sealed class StageDef
    {
        public string Id;
        public string Name;
        public string Intro;
        public string[] Waves;  // EnemyDef.Id 목록 (웨이브 하나 = 적 1마리)
        public int ClearMemory;
    }

    /// <summary>
    /// 게임 데이터 표. 수치는 전부 [임시] — 기준 문서: Docs/Design/TextSlayers_Concept_v0.1.md
    /// </summary>
    public static class GameData
    {
        // ── 전투 공통 [임시] ──
        public const float GaugeMax = 100f;
        public const float GaugePerSpd = 5f;        // 초당 게이지 = SPD × 이 값 (SPD 10 → 2초에 1번)
        public const int MpRegenPerAction = 2;
        public const float DefBase = 100f;          // 받는 피해 × DefBase / (DefBase + DEF)
        public const float DamageVariance = 0.10f;  // 피해 ±10%
        public const float GuardReduction = 0.5f;   // 방어 중 받는 피해 배율
        public const float WaveInterval = 1.2f;     // 웨이브 사이 대기(초)
        public const float DefeatRewardRate = 0.5f; // 패배 · 후퇴 시 받는 보상 비율

        // ── 보스 강타 [임시] ──
        public const float BossHeavyChance = 0.25f; // 행동마다 강타 준비 확률
        public const float BossHeavyCoef = 1.8f;

        // ── 성장 [임시] ──
        public static readonly Stats ErnBase = new Stats(100, 30, 10, 3, 10f, 0.10f, 1.5f);
        public static readonly Stats ErnPerLevel = new Stats(12, 3, 2, 1, 0.5f, 0f, 0f);
        public const float ExpBase = 20f;           // 필요 경험치 = ExpBase × Lv^ExpPow
        public const float ExpPow = 1.5f;
        public const int MaxLevel = 99;

        public static Stats ErnStatsAt(int level)
        {
            int n = Mathf.Clamp(level, 1, MaxLevel) - 1;
            Stats b = ErnBase, p = ErnPerLevel;
            return new Stats(
                b.HP + p.HP * n,
                b.MP + p.MP * n,
                b.ATK + p.ATK * n,
                b.DEF + p.DEF * n,
                b.SPD + p.SPD * n,
                b.CRIT + p.CRIT * n,
                b.CRITDMG + p.CRITDMG * n);
        }

        public static int ExpToNext(int level) => Mathf.RoundToInt(ExpBase * Mathf.Pow(level, ExpPow));

        // ── 에른 행동 (키 1~4) [임시] ──
        public static readonly SkillDef[] ErnSkills =
        {
            new SkillDef { Id = "slash", Name = "베기", Key = "1", MpCost = 0, Kind = ActionKind.Attack,
                Coef = 1.0f, Desc = "100% 1타" },
            new SkillDef { Id = "triple_slash", Name = "트리플 슬래시", Key = "2", MpCost = 8, Kind = ActionKind.Attack,
                Coef = 0.55f, Hits = 3, Desc = "55% × 3타, 타마다 치명타 판정" },
            new SkillDef { Id = "fire_blade", Name = "파이어 블레이드", Key = "3", MpCost = 14, Kind = ActionKind.Attack,
                Coef = 1.7f, BonusCrit = 0.2f, Desc = "170% 1타, 치명타 확률 +20%" },
            new SkillDef { Id = "guard", Name = "방어", Key = "4", MpCost = 0, Kind = ActionKind.Guard,
                MpGain = 6, Desc = "다음 피격 피해 50% 감소, MP +6" },
        };

        // ── 적 [임시] ──
        static readonly EnemyDef[] EnemyTable =
        {
            new EnemyDef { Id = "goblin", Name = "고블린", Stats = new Stats(40, 0, 6, 2, 9f, 0.05f, 1.5f),
                Exp = 8, Gold = 5, AppearText = "덤불을 헤치고 고블린이 튀어나왔다." },
            new EnemyDef { Id = "goblin_archer", Name = "고블린 궁수", Stats = new Stats(35, 0, 9, 1, 11f, 0.05f, 1.5f),
                Exp = 9, Gold = 6, AppearText = "나무 위에서 고블린 궁수가 활을 겨눈다." },
            new EnemyDef { Id = "hobgoblin", Name = "홉 고블린", Stats = new Stats(120, 0, 10, 5, 8f, 0.05f, 1.5f),
                Exp = 40, Gold = 30, IsBoss = true, AppearText = "땅이 울린다. 홉 고블린이 몽둥이를 끌며 다가온다!" },

            // 테스트 맵 전용 (v0.2 구조 확인, 사용자 2026-11-26). 공격력 0, 체력 무한(FieldEnemyDef.Immortal). 보상 없음.
            new EnemyDef { Id = "test_dummy_melee", Name = "테스트 더미 1", Stats = new Stats(1000, 0, 0, 0, 9f, 0f, 1.5f),
                Exp = 0, Gold = 0, AppearText = "근거리 테스트 더미." },
            new EnemyDef { Id = "test_dummy_ranged", Name = "테스트 더미 2", Stats = new Stats(1000, 0, 0, 0, 9f, 0f, 1.5f),
                Exp = 0, Gold = 0, AppearText = "원거리 테스트 더미." },
        };

        static Dictionary<string, EnemyDef> enemyById;

        public static EnemyDef Enemy(string id)
        {
            if (enemyById == null)
            {
                enemyById = new Dictionary<string, EnemyDef>();
                foreach (var e in EnemyTable) enemyById[e.Id] = e;
            }
            if (enemyById.TryGetValue(id, out var def)) return def;
            Debug.LogError($"[TextSlayers] 없는 적 ID: {id}");
            return EnemyTable[0];
        }

        // ── 스테이지 [임시] ──
        public static readonly StageDef[] Stages =
        {
            new StageDef
            {
                Id = "stage1", Name = "고블린 숲",
                Intro = "축축한 숲길. 고블린들의 웃음소리가 가까워진다.",
                Waves = new[] { "goblin", "goblin_archer", "goblin", "hobgoblin" },
                ClearMemory = 1,
            },
        };
    }
}
