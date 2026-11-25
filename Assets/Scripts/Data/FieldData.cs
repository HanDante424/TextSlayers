using System.Collections.Generic;
using UnityEngine;

namespace TextSlayers
{
    // ── 기획 v0.2 (실시간 필드 오토배틀러). 수치는 전부 [임시] ──

    public enum SkillTag { Attack, Defense, Heal, Support }                       // 스킬 · 패시브의 역할 태그
    public enum Role { Tank, Dealer, Healer, Supporter }                          // 역할군 (표기용, 효과 없음)
    public enum Strategy { Hold, Charge, Flank, Kite, Escort }                    // 전략방식 (움직임 AI)
    public enum SkillEffect { Damage, Guard, Taunt, Heal, AtkBuff,
                              Spin, Smash, DarkWave, Pulse, Grasp, Harvest }   // 뒤 6개: 스티움 고유 (v0.2 스킬 문서)
    public enum TargetRule { Enemy, EnemyArea, Ally, AllyAll, Self }

    public sealed class FieldSkillDef
    {
        public string Id;
        public string Name;
        public SkillTag Tag;
        public SkillEffect Effect;
        public TargetRule Target;
        public float Range;      // 0 이면 유닛 자신의 기본 사거리
        public float Cooldown;   // 초
        public float Coef;       // 피해 · 회복 계수 (ATK 배율, 1타당)
        public int Hits = 1;
        public float BonusCrit;
        public float Duration;   // 방어 · 도발 · 버프 지속(초)
        public float Value;      // 버프 수치 (ATK +0.15 = +15%)
        public string Desc;

        // v0.2 스킬 피해 구조 (스킬 문서 4장): 물리 = Coef(공격력 계수), 마법 = MagicBase + 마력 × MagicCoef
        public float MagicBase;
        public float MagicCoef;
        public float Radius;     // 범위 스킬 반지름 (유닛)
        /// <summary>사용자가 수치를 확정한 스킬. false 면 쿨타임은 FieldConst.UndecidedCooldown (사용자 2026-11-26).</summary>
        public bool Decided;
        public float EffectiveCooldown => Decided ? Cooldown : FieldConst.UndecidedCooldown;
    }

    public sealed class FieldPassiveDef
    {
        public string Id;
        public string Name;
        public SkillTag Tag;
        public float DefMul = 1f;
        public float CritAdd;
        public float MovMul = 1f;
        public float RegenPct;   // 초당 최대 HP 비율
        public string Desc;
    }

    public sealed class CharacterDef
    {
        public string Id;
        public string Name;
        public string Kind;                // 사용자가 정한 유형 표기 (예: 근거리 딜탱)
        public Role BaseRole;
        public Strategy DefaultStrategy;
        public Stats Base;                 // Lv.1 스탯 (MP 는 쓰지 않음)
        public float Mov;                  // 이동 속도 (유닛/초)
        public float Range;                // 기본 공격 사거리 (유닛)
        public string[] Skills;            // 액티브 3
        public string Passive;             // 패시브 1
        /// <summary>수치가 정해진 캐릭터. false 면 주는 피해가 전부 0 (사용자 2026-11-26).</summary>
        public bool NumbersSet;
    }

    public sealed class FieldEnemyDef
    {
        public string Id;                  // GameData.Enemy 의 ID 와 같다 (스탯 · 보상은 거기서)
        public Strategy Strategy;
        public float Mov;
        public float Range;
        public string[] Skills = new string[0];
        public bool Immortal;              // 체력 무한 (테스트 더미). 피해는 기록하지만 HP 가 줄지 않는다.
    }

    public sealed class FieldStageDef
    {
        public string Id;
        public string Name;
        public string Intro;
        public string[][] Waves;           // 웨이브마다 EnemyDef.Id 목록
        public int ClearMemory;
        public bool IsTest;                // 테스트 맵: 시간 제한 없음, 시뮬레이션 제외, 나가기로만 끝남
    }

    /// <summary>편성 한 칸 (캐릭터 + 액티브 3 + 패시브 1 + 전략방식).</summary>
    public sealed class PartySlot
    {
        public CharacterDef Def;
        public Strategy Strategy;
        public string[] Skills;
        public string Passive;
    }

    /// <summary>필드 전투 공통 상수 [임시].</summary>
    public static class FieldConst
    {
        public const float Dt = 0.05f;                 // 시뮬레이션 고정 스텝(초)
        public const float Width = 20f;
        public const float Height = 10f;
        public const float Radius = 0.5f;              // 유닛 반지름
        public const float AttackPerSpd = 0.1f;        // 행동 간격 = 1 / (SPD × 이 값) → SPD 10 이면 1초
        public const float FleeDist = 3f;              // 도주 · 견제: 이 거리 안에 적이 붙으면 이탈
        public const float EscortDist = 2f;            // 호위: 보호 대상과 유지하는 거리
        public const float ChargeAggro = 2f;           // 돌진 어그로 배율
        public const float FlankAggro = 0.5f;          // 후방 침투 어그로 배율
        public const float RangedThreshold = 4f;       // 이 사거리 이상이면 원거리 유닛으로 본다
        public const float RangedPriority = 3f;        // 후방 침투가 원거리 유닛을 노리는 가중
        public const float FlankWaypointDist = 1.5f;
        public const float FlankCloseX = 2f;           // 목표와 x 가 이 안으로 들어오면 우회 종료
        public const float ApproachRatio = 0.9f;       // 사거리의 이 비율까지 접근
        public const float PlayerStartX = 2f;
        public const float EnemyStartX = 18f;
        public const float WaveInterval = 2f;
        public const float TimeLimit = 300f;           // 이 시간 넘으면 패배(멈춤 방지)
        public const float HealBelow = 0.7f;           // 회복 스킬은 HP 비율이 이 미만인 아군에게
        public const int MaxStepsPerUpdate = 200;
        public const int MaxPartySize = 5;
        public const float UndecidedCooldown = 10f;    // 수치 미정 스킬의 쿨타임 (사용자 2026-11-26)
        public const float ProjectileSpeed = 10f;      // 투사체 속도 (유닛/초) [임시]
        public const float ProjectileHitRadius = 0.6f; // 투사체 판정 반지름 [임시]             // 테스트 단계 출전 인원 (사용자 2026-11-26). 기획 최대 7 은 추후
    }

    /// <summary>필드 전투 데이터 표. 기준 문서: Docs/Design/TextSlayers_Concept_v0.2.md</summary>
    public static class FieldData
    {
        // 레벨당 성장 [임시] (모든 캐릭터 공통, 캐릭터별 성장은 사용자 확정 후)
        public static readonly Stats PerLevel = new Stats(12, 0, 2, 1, 0.5f, 0f, 0f);

        // ── 액티브 [임시] ──
        static readonly FieldSkillDef[] SkillTable =
        {
            new FieldSkillDef { Id = "slash", Name = "베기", Tag = SkillTag.Attack, Effect = SkillEffect.Damage,
                Target = TargetRule.Enemy, Range = 1.5f, Cooldown = 1.5f, Coef = 1.0f, Desc = "100% 1타" },
            new FieldSkillDef { Id = "triple_slash", Name = "트리플 슬래시", Tag = SkillTag.Attack, Effect = SkillEffect.Damage,
                Target = TargetRule.Enemy, Range = 1.5f, Cooldown = 5f, Coef = 0.55f, Hits = 3, Desc = "55% × 3타" },
            new FieldSkillDef { Id = "fire_blade", Name = "파이어 블레이드", Tag = SkillTag.Attack, Effect = SkillEffect.Damage,
                Target = TargetRule.Enemy, Range = 1.5f, Cooldown = 7f, Coef = 1.7f, BonusCrit = 0.2f, Desc = "170%, 치명타 확률 +20%" },
            new FieldSkillDef { Id = "guard", Name = "방어", Tag = SkillTag.Defense, Effect = SkillEffect.Guard,
                Target = TargetRule.Self, Range = 3f, Cooldown = 8f, Duration = 3f, Desc = "3초간 받는 피해 50% 감소 (적이 3 안에 있을 때)" },
            new FieldSkillDef { Id = "taunt", Name = "도발", Tag = SkillTag.Defense, Effect = SkillEffect.Taunt,
                Target = TargetRule.EnemyArea, Range = 4f, Cooldown = 10f, Duration = 2f, Desc = "주변 적의 타겟을 2초간 자신에게 고정" },
            new FieldSkillDef { Id = "shot", Name = "사격", Tag = SkillTag.Attack, Effect = SkillEffect.Damage,
                Target = TargetRule.Enemy, Range = 7f, Cooldown = 2.5f, Coef = 1.2f, Desc = "120% 1타 (원거리)" },
            new FieldSkillDef { Id = "power_shot", Name = "강사격", Tag = SkillTag.Attack, Effect = SkillEffect.Damage,
                Target = TargetRule.Enemy, Range = 8f, Cooldown = 6f, Coef = 1.6f, BonusCrit = 0.1f, Desc = "160%, 치명타 확률 +10% (원거리)" },
            new FieldSkillDef { Id = "first_aid", Name = "응급 처치", Tag = SkillTag.Heal, Effect = SkillEffect.Heal,
                Target = TargetRule.Ally, Range = 6f, Cooldown = 6f, Coef = 0.8f, Desc = "ATK × 80% 회복 (HP 70% 미만 아군)" },
            new FieldSkillDef { Id = "cheer", Name = "격려", Tag = SkillTag.Support, Effect = SkillEffect.AtkBuff,
                Target = TargetRule.AllyAll, Cooldown = 12f, Duration = 4f, Value = 0.15f, Desc = "4초간 아군 전체 ATK +15%" },
            new FieldSkillDef { Id = "heavy_blow", Name = "강타", Tag = SkillTag.Attack, Effect = SkillEffect.Damage,
                Target = TargetRule.Enemy, Range = 1.8f, Cooldown = 6f, Coef = 1.8f, Desc = "180% 1타 (보스)" },

            // ── 스티움 카터렉 (스킬 문서 4장, 기본 상태만. 어둠 각성 · 밀쳐내기 · 끌어당기기 · 기절은 미구현) ──
            // 피해 · 쿨타임은 사용자 확정값. 범위 · 사거리는 미정이라 [임시].
            new FieldSkillDef { Id = "stium_spin", Name = "회전베기", Tag = SkillTag.Attack, Effect = SkillEffect.Spin,
                Target = TargetRule.EnemyArea, Radius = 2f, Cooldown = 6f, Coef = 1.0f, MagicBase = 80f, MagicCoef = 0.5f,
                Decided = true, Desc = "전방위: 공격력 100% 물리 + (80 + 마력 50%) 마법" },
            new FieldSkillDef { Id = "stium_smash", Name = "강격검", Tag = SkillTag.Attack, Effect = SkillEffect.Smash,
                Target = TargetRule.Enemy, Range = 1.8f, Radius = 1.2f, Cooldown = 12f, Coef = 1.5f, MagicBase = 90f, MagicCoef = 0.6f,
                Decided = true, Desc = "대상: 공격력 150% 물리 + (90 + 마력 60%) 마법" },
            new FieldSkillDef { Id = "stium_darkwave", Name = "어둠의 검기", Tag = SkillTag.Attack, Effect = SkillEffect.DarkWave,
                Target = TargetRule.Enemy, Range = 8f, Cooldown = 10f, MagicBase = 60f, MagicCoef = 0.75f,
                Decided = true, Desc = "검기가 처음 적중한 적에게 (60 + 마력 75%) 마법" },
            new FieldSkillDef { Id = "stium_pulse", Name = "어둠의 파동", Tag = SkillTag.Attack, Effect = SkillEffect.Pulse,
                Target = TargetRule.EnemyArea, Radius = 2.5f, Cooldown = 11f, MagicBase = 80f, MagicCoef = 0.6f,
                Decided = true, Desc = "근처: (80 + 마력 60%) 마법 (밀쳐내기 미구현)" },
            new FieldSkillDef { Id = "stium_grasp", Name = "어둠의 손아귀", Tag = SkillTag.Attack, Effect = SkillEffect.Grasp,
                Target = TargetRule.EnemyArea, Radius = 4f, Cooldown = 10f,
                Desc = "어둠을 퍼트린 뒤 끌어당김 (수치 · 끌어당기기 미정)" },
            new FieldSkillDef { Id = "stium_harvest", Name = "어둠 거두기", Tag = SkillTag.Attack, Effect = SkillEffect.Harvest,
                Target = TargetRule.EnemyArea, Radius = 6f, Cooldown = 10f,
                Desc = "어둠 잠식 소모 (잠식 · 수치 미구현)" },
        };

        // ── 패시브 [임시] ──
        static readonly FieldPassiveDef[] PassiveTable =
        {
            new FieldPassiveDef { Id = "sturdy", Name = "단단한 몸", Tag = SkillTag.Defense, DefMul = 1.2f, Desc = "DEF +20%" },
            new FieldPassiveDef { Id = "keen_eye", Name = "날카로운 눈", Tag = SkillTag.Attack, CritAdd = 0.1f, Desc = "CRIT +10%p" },
            new FieldPassiveDef { Id = "regen", Name = "재생", Tag = SkillTag.Heal, RegenPct = 0.01f, Desc = "초당 최대 HP 1% 회복" },
            new FieldPassiveDef { Id = "swift", Name = "질주", Tag = SkillTag.Support, MovMul = 1.15f, Desc = "MOV +15%" },
        };

        // ── 캐릭터 [임시] ──  이름 · 유형 · 역할은 사용자 확정, 스탯 · 스킬 구성은 자리 표시
        public static readonly CharacterDef[] Roster =
        {
            // 스티움: 기준 스탯 체력 1500 · 공격력 30 · 마력 30, 기본 공격 0.6회/초 (= SPD 6) — 사용자 확정 2026-11-25.
            // 이펙트 확인용으로 고유 액티브 6개를 모두 장착 [임시] (장착 3개 규칙 · 스킬 선택 화면은 추후). 패시브 3종은 미구현.
            new CharacterDef { Id = "stium", Name = "스티움 카터렉", Kind = "근거리 딜탱, 탱커", BaseRole = Role.Tank,
                DefaultStrategy = Strategy.Charge, Base = new Stats(1500, 0, 30, 0, 6f, 0.05f, 1.5f) { MAG = 30 }, Mov = 3.0f, Range = 1.5f,
                Skills = new[] { "stium_spin", "stium_smash", "stium_darkwave", "stium_pulse", "stium_grasp", "stium_harvest" },
                Passive = "sturdy", NumbersSet = true },
            new CharacterDef { Id = "ern", Name = "에른 스트레이", Kind = "근거리 딜탱", BaseRole = Role.Dealer,
                DefaultStrategy = Strategy.Flank, Base = new Stats(110, 0, 11, 4, 10f, 0.10f, 1.5f), Mov = 3.5f, Range = 1.5f,
                Skills = new[] { "slash", "triple_slash", "fire_blade" }, Passive = "keen_eye" },
            new CharacterDef { Id = "mel", Name = "멜 발렌티나", Kind = "서포터, 원거리 딜러", BaseRole = Role.Supporter,
                DefaultStrategy = Strategy.Kite, Base = new Stats(80, 0, 9, 2, 10f, 0.05f, 1.5f), Mov = 3.0f, Range = 7f,
                Skills = new[] { "shot", "first_aid", "cheer" }, Passive = "swift" },
            new CharacterDef { Id = "vendetta", Name = "벤데타", Kind = "원거리 딜러", BaseRole = Role.Dealer,
                DefaultStrategy = Strategy.Kite, Base = new Stats(75, 0, 12, 2, 10f, 0.10f, 1.6f), Mov = 3.0f, Range = 8f,
                Skills = new[] { "shot", "power_shot", "guard" }, Passive = "keen_eye" },
            new CharacterDef { Id = "paracelsus", Name = "파라켈수스", Kind = "서포터", BaseRole = Role.Supporter,
                DefaultStrategy = Strategy.Escort, Base = new Stats(85, 0, 6, 3, 9f, 0.05f, 1.5f), Mov = 3.0f, Range = 6f,
                Skills = new[] { "first_aid", "cheer", "shot" }, Passive = "regen" },

            // 나머지 43명: 이름 · 유형 · 기본 역할 · 기본 전략은 기획서 5장 표 그대로.
            // 스탯 · 스킬은 첫 번째 유형의 틀(Template)로 채운 자리 표시 [임시] — 고유 스킬 구현 전 구조 테스트용.
            T("c06", "메일 보레아", "근거리 딜탱", Role.Dealer, Strategy.Charge, Tpl.Bruiser),
            T("c07", "모모로이 세이린", "탱커, 서포터", Role.Tank, Strategy.Charge, Tpl.Tank),
            T("c08", "투견", "근거리 딜탱", Role.Dealer, Strategy.Charge, Tpl.Bruiser),
            T("c09", "마리스 레지나", "암살자", Role.Dealer, Strategy.Flank, Tpl.Assassin),
            T("c10", "레일 루카스", "근거리 딜러", Role.Dealer, Strategy.Flank, Tpl.Melee),
            T("c11", "베들렌 메릴", "근거리 딜탱", Role.Dealer, Strategy.Charge, Tpl.Bruiser),
            T("c12", "릭 엔더빌", "근거리 딜러", Role.Dealer, Strategy.Flank, Tpl.Melee),
            T("c13", "아그니", "근거리 딜러", Role.Dealer, Strategy.Charge, Tpl.Melee),
            T("c14", "노와", "암살자", Role.Dealer, Strategy.Flank, Tpl.Assassin),
            T("c15", "슈진 잔엔", "근거리 딜러", Role.Dealer, Strategy.Charge, Tpl.Melee),
            T("c16", "청란", "근거리 딜탱", Role.Dealer, Strategy.Charge, Tpl.Bruiser),
            T("c17", "한니발 멜카르티드", "암살자", Role.Dealer, Strategy.Flank, Tpl.Assassin),
            T("c18", "이게르투스", "탱커", Role.Tank, Strategy.Charge, Tpl.Tank),
            T("c19", "요월", "근거리 딜러", Role.Dealer, Strategy.Charge, Tpl.Melee),
            T("c20", "환백", "암살자", Role.Dealer, Strategy.Flank, Tpl.Assassin),
            T("c21", "흑영", "근거리 딜탱", Role.Dealer, Strategy.Charge, Tpl.Bruiser),
            T("c22", "야월", "암살자", Role.Dealer, Strategy.Flank, Tpl.Assassin),
            T("c23", "분진", "암살자", Role.Dealer, Strategy.Flank, Tpl.Assassin),
            T("c24", "아가트라", "근거리 딜탱", Role.Dealer, Strategy.Charge, Tpl.Bruiser),
            T("c25", "켈리아 피리뉴아", "근거리 딜러", Role.Dealer, Strategy.Charge, Tpl.Melee),
            T("c26", "렐리스 페네투아", "서포터", Role.Supporter, Strategy.Escort, Tpl.Support),
            T("c27", "리카르도 레콩키스타", "탱커", Role.Tank, Strategy.Charge, Tpl.Tank),
            T("c28", "알렉스", "탱커, 서포터", Role.Tank, Strategy.Charge, Tpl.Tank),
            T("c29", "앨리스", "근거리 딜러", Role.Dealer, Strategy.Flank, Tpl.Melee),
            T("c30", "베네딕트 테너브리움", "탱커", Role.Tank, Strategy.Charge, Tpl.Tank),
            T("c31", "에퀴녹스 파리에눔", "암살자", Role.Dealer, Strategy.Flank, Tpl.Assassin),
            T("c32", "라바다", "근거리 딜탱, 서포터", Role.Dealer, Strategy.Escort, Tpl.Bruiser),
            T("c33", "엘 카벤", "근거리 딜탱, 서포터", Role.Dealer, Strategy.Escort, Tpl.Bruiser),
            T("c34", "사우윈 키르윈", "탱커", Role.Tank, Strategy.Charge, Tpl.Tank),
            T("c35", "이믈록 사비아", "원거리 딜러", Role.Dealer, Strategy.Kite, Tpl.Ranged),
            T("c36", "발타너 트래헌", "근거리 딜러", Role.Dealer, Strategy.Flank, Tpl.Melee),
            T("c37", "루나사 키아라", "원거리 딜러, 서포터", Role.Dealer, Strategy.Kite, Tpl.Ranged),
            T("c38", "레온하르트", "근거리 딜탱, 탱커, 서포터", Role.Dealer, Strategy.Charge, Tpl.Bruiser),
            T("c39", "드로미", "근거리 딜탱", Role.Dealer, Strategy.Charge, Tpl.Bruiser),
            T("c40", "펜리르", "근거리 딜탱", Role.Dealer, Strategy.Charge, Tpl.Bruiser),
            T("c41", "사하라", "근거리 딜탱, 서포터", Role.Dealer, Strategy.Escort, Tpl.Bruiser),
            T("c42", "갈 빌랜드", "탱커, 서포터", Role.Tank, Strategy.Charge, Tpl.Tank),
            T("c43", "니그레도", "원거리 딜러", Role.Dealer, Strategy.Kite, Tpl.Ranged),
            T("c44", "알베도", "암살자", Role.Dealer, Strategy.Flank, Tpl.Assassin),
            T("c45", "치트리니타스", "탱커", Role.Tank, Strategy.Charge, Tpl.Tank),
            T("c46", "비리디타스", "서포터", Role.Supporter, Strategy.Escort, Tpl.Support),
            T("c47", "루베도", "원거리 딜러", Role.Dealer, Strategy.Kite, Tpl.Ranged),
            T("c48", "가니악 벤데스트라", "근거리 딜탱", Role.Dealer, Strategy.Charge, Tpl.Bruiser),
        };

        /// <summary>자리 표시 캐릭터의 스탯 · 스킬 틀 (첫 번째 유형 기준) [임시].</summary>
        enum Tpl { Bruiser, Melee, Tank, Assassin, Ranged, Support }

        // 정적 필드 초기화 순서에 기대지 않도록 값은 메서드 안에서 만든다.
        static CharacterDef T(string id, string name, string kind, Role role, Strategy strategy, Tpl tpl)
        {
            var c = new CharacterDef { Id = id, Name = name, Kind = kind, BaseRole = role, DefaultStrategy = strategy };
            switch (tpl)
            {
                case Tpl.Bruiser:   // 근거리 딜탱
                    c.Base = new Stats(120, 0, 10, 5, 9.5f, 0.08f, 1.5f); c.Mov = 3.2f; c.Range = 1.5f;
                    c.Skills = new[] { "slash", "triple_slash", "guard" }; c.Passive = "sturdy"; break;
                case Tpl.Melee:     // 근거리 딜러
                    c.Base = new Stats(100, 0, 12, 3, 10f, 0.10f, 1.5f); c.Mov = 3.5f; c.Range = 1.5f;
                    c.Skills = new[] { "slash", "triple_slash", "fire_blade" }; c.Passive = "keen_eye"; break;
                case Tpl.Tank:      // 탱커
                    c.Base = new Stats(150, 0, 7, 7, 8.5f, 0.05f, 1.5f); c.Mov = 2.8f; c.Range = 1.5f;
                    c.Skills = new[] { "slash", "guard", "taunt" }; c.Passive = "sturdy"; break;
                case Tpl.Assassin:  // 암살자
                    c.Base = new Stats(85, 0, 13, 2, 11f, 0.15f, 1.6f); c.Mov = 4.0f; c.Range = 1.5f;
                    c.Skills = new[] { "slash", "triple_slash", "fire_blade" }; c.Passive = "swift"; break;
                case Tpl.Ranged:    // 원거리 딜러
                    c.Base = new Stats(75, 0, 12, 2, 10f, 0.10f, 1.6f); c.Mov = 3.0f; c.Range = 8f;
                    c.Skills = new[] { "shot", "power_shot", "guard" }; c.Passive = "keen_eye"; break;
                default:            // 서포터
                    c.Base = new Stats(85, 0, 7, 3, 9f, 0.05f, 1.5f); c.Mov = 3.0f; c.Range = 6f;
                    c.Skills = new[] { "first_aid", "cheer", "shot" }; c.Passive = "regen"; break;
            }
            return c;
        }

        // ── 적 (스탯 · 보상은 GameData.Enemy) [임시] ──
        static readonly FieldEnemyDef[] EnemyTable =
        {
            new FieldEnemyDef { Id = "goblin", Strategy = Strategy.Charge, Mov = 2.8f, Range = 1.5f },
            new FieldEnemyDef { Id = "goblin_archer", Strategy = Strategy.Kite, Mov = 2.5f, Range = 7f },
            new FieldEnemyDef { Id = "hobgoblin", Strategy = Strategy.Charge, Mov = 2.2f, Range = 1.8f,
                Skills = new[] { "heavy_blow" } },
            // 테스트 더미: 근거리는 돌진, 원거리는 도주 · 견제로 움직인다 [임시]
            new FieldEnemyDef { Id = "test_dummy_melee", Strategy = Strategy.Charge, Mov = 2.5f, Range = 1.5f, Immortal = true },
            new FieldEnemyDef { Id = "test_dummy_ranged", Strategy = Strategy.Kite, Mov = 2.5f, Range = 7f, Immortal = true },
        };

        // ── 스테이지 [임시] ──
        public static readonly FieldStageDef[] Stages =
        {
            // 게임 구조 테스트 (사용자 2026-11-26): 테스트 더미 1 × 4, 테스트 더미 2 × 3. 끝나지 않으므로 나가기로 종료.
            new FieldStageDef
            {
                Id = "test_map", Name = "디버그 맵",
                Intro = "공격하지 않는 더미들이 서 있다. 쓰러지지도 않는다. (아래 디버그 도구로 배치 · 제거 · AI 끄기)",
                Waves = new[]
                {
                    new[] { "test_dummy_melee", "test_dummy_melee", "test_dummy_melee", "test_dummy_melee",
                            "test_dummy_ranged", "test_dummy_ranged", "test_dummy_ranged" },
                },
                ClearMemory = 0,
                IsTest = true,
            },
            new FieldStageDef
            {
                Id = "stage1", Name = "고블린 숲",
                Intro = "축축한 숲길. 고블린들의 웃음소리가 가까워진다.",
                Waves = new[]
                {
                    new[] { "goblin", "goblin", "goblin" },
                    new[] { "goblin", "goblin", "goblin_archer" },
                    new[] { "goblin", "goblin", "goblin_archer", "goblin_archer" },
                    new[] { "hobgoblin", "goblin", "goblin" },
                },
                ClearMemory = 1,
            },
        };

        // ── 조회 ──
        static Dictionary<string, FieldSkillDef> skillById;
        static Dictionary<string, FieldPassiveDef> passiveById;
        static Dictionary<string, FieldEnemyDef> enemyById;

        /// <summary>없으면 null.</summary>
        public static FieldSkillDef Skill(string id)
        {
            if (skillById == null)
            {
                skillById = new Dictionary<string, FieldSkillDef>();
                foreach (var s in SkillTable) skillById[s.Id] = s;
            }
            if (id != null && skillById.TryGetValue(id, out var def)) return def;
            return null;
        }

        /// <summary>없으면 null.</summary>
        public static FieldPassiveDef Passive(string id)
        {
            if (passiveById == null)
            {
                passiveById = new Dictionary<string, FieldPassiveDef>();
                foreach (var p in PassiveTable) passiveById[p.Id] = p;
            }
            if (id != null && passiveById.TryGetValue(id, out var def)) return def;
            return null;
        }

        public static FieldEnemyDef Enemy(string id)
        {
            if (enemyById == null)
            {
                enemyById = new Dictionary<string, FieldEnemyDef>();
                foreach (var e in EnemyTable) enemyById[e.Id] = e;
            }
            if (enemyById.TryGetValue(id, out var def)) return def;
            Debug.LogError($"[TextSlayers] 없는 필드 적 ID: {id}");
            return EnemyTable[0];
        }

        public static CharacterDef Character(string id)
        {
            foreach (var c in Roster) if (c.Id == id) return c;
            Debug.LogError($"[TextSlayers] 없는 캐릭터 ID: {id}");
            return Roster[0];
        }

        /// <summary>캐릭터 기본 구성으로 만든 편성 한 칸.</summary>
        public static PartySlot DefaultSlot(string characterId)
        {
            var c = Character(characterId);
            return new PartySlot
            {
                Def = c,
                Strategy = c.DefaultStrategy,
                Skills = (string[])c.Skills.Clone(),
                Passive = c.Passive,
            };
        }

        // ── 스탯 ──
        public static Stats StatsAt(CharacterDef d, int level)
        {
            int n = Mathf.Clamp(level, 1, GameData.MaxLevel) - 1;
            Stats b = d.Base, p = PerLevel;
            return new Stats(
                b.HP + p.HP * n, 0,
                b.ATK + p.ATK * n,
                b.DEF + p.DEF * n,
                b.SPD + p.SPD * n,
                b.CRIT, b.CRITDMG) { MAG = b.MAG, RES = b.RES };
        }

        /// <summary>패시브의 스탯 보정 (DEF · CRIT). MOV · 재생은 전투 쪽에서 처리.</summary>
        public static Stats ApplyPassive(Stats s, FieldPassiveDef p)
        {
            if (p == null) return s;
            return new Stats(s.HP, s.MP, s.ATK, Mathf.RoundToInt(s.DEF * p.DefMul), s.SPD, s.CRIT + p.CritAdd, s.CRITDMG)
                { MAG = s.MAG, RES = s.RES };
        }

        // ── 역할군 (표기용) ──
        public static Role TagRole(SkillTag tag)
        {
            switch (tag)
            {
                case SkillTag.Defense: return Role.Tank;
                case SkillTag.Heal: return Role.Healer;
                case SkillTag.Support: return Role.Supporter;
                default: return Role.Dealer;
            }
        }

        /// <summary>전략방식이 주는 역할 (Hold 는 없음).</summary>
        public static Role? StrategyRole(Strategy s)
        {
            switch (s)
            {
                case Strategy.Charge: return Role.Tank;
                case Strategy.Flank: return Role.Dealer;
                case Strategy.Kite: return Role.Dealer;
                case Strategy.Escort: return Role.Supporter;
                default: return null;
            }
        }

        /// <summary>
        /// 현재 역할 = 점수 합 최고. 전략방식 2점, 액티브 각 1점, 패시브 1점 (기획 v0.2 5장).
        /// 동률이면 전략방식의 역할 → 캐릭터 기본 역할 → 앞선 역할 순 [해석].
        /// </summary>
        public static Role CurrentRole(CharacterDef c, Strategy strategy, string[] skills, string passiveId)
        {
            var score = new int[4];
            Role? stratRole = StrategyRole(strategy);
            if (stratRole.HasValue) score[(int)stratRole.Value] += 2;
            if (skills != null)
            {
                foreach (var id in skills)
                {
                    var s = Skill(id);
                    if (s != null) score[(int)TagRole(s.Tag)]++;
                }
            }
            var p = Passive(passiveId);
            if (p != null) score[(int)TagRole(p.Tag)]++;

            int best = Mathf.Max(Mathf.Max(score[0], score[1]), Mathf.Max(score[2], score[3]));
            if (stratRole.HasValue && score[(int)stratRole.Value] == best) return stratRole.Value;
            if (score[(int)c.BaseRole] == best) return c.BaseRole;
            for (int i = 0; i < 4; i++) if (score[i] == best) return (Role)i;
            return c.BaseRole;
        }

        // ── 표시용 이름 ──
        public static string RoleName(Role r)
        {
            switch (r)
            {
                case Role.Tank: return "탱커";
                case Role.Dealer: return "딜러";
                case Role.Healer: return "힐러";
                default: return "서포터";
            }
        }

        public static string StrategyName(Strategy s)
        {
            switch (s)
            {
                case Strategy.Charge: return "돌진";
                case Strategy.Flank: return "후방 침투";
                case Strategy.Kite: return "도주 · 견제";
                case Strategy.Escort: return "호위";
                default: return "진형 유지";
            }
        }
    }
}
