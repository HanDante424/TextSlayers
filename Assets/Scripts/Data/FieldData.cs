using System.Collections.Generic;
using UnityEngine;

namespace TextSlayers
{
    // ── 기획 v0.2 (실시간 필드 오토배틀러). 수치는 전부 [임시] ──

    public enum SkillTag { Attack, Defense, Heal, Support }                       // 스킬 · 패시브의 역할 태그
    public enum Role { Tank, Dealer, Healer, Supporter }                          // 역할군 (표기용, 효과 없음)
    public enum Strategy { Hold, Charge, Flank, Kite, Escort, Backline }          // 전략방식 (움직임 AI). Backline = 후방 우선 공격

    /// <summary>기본 공격 모양 (사용자 2026-11-26). 근거리는 휘두르기 이펙트, 원거리는 투사체.</summary>
    public enum BasicKind { Sword, GreatSword, Halberd, Katana, Fist, Claw, Cross, FireSword, DarkSword, Glutton, IceShard, Pistol, Potion, DarkOrb,
                            HSlash, Bolt, None }   // HSlash = 횡베기 (근접 더미), Bolt = 파티클 탄 (원거리 더미), None = 이펙트 없음

    /// <summary>캐릭터 파티클 테마 (스킬 · 기본 공격 이펙트에 섞이는 입자).</summary>
    public enum FxTheme { None, Fire, Dark, Moon, Holy, Ice, Frost, Spirit, Potion, Talisman, Gold, Steel, Black, Shadow, Gray }
    public enum SkillEffect { Damage, Guard, Taunt, Heal, AtkBuff,
                              Spin, Smash, DarkWave, Pulse, Grasp, Harvest }   // 뒤 6개: 스티움 고유 (v0.2 스킬 문서)
    public enum TargetRule { Enemy, EnemyArea, Ally, AllyAll, Self }

    // ── 컨셉 스킬 공통 실행 (ConceptSkills.cs). Shape 가 None 이 아니면 FieldBattle.UseGeneric 이 처리 ──
    public enum SkillShape
    {
        None,          // 예전 방식 (SkillEffect 로 처리)
        Around,        // 자신 주변 (Radius)
        Target,        // 대상 1명
        TargetArea,    // 대상 위치 주변 (Radius)
        Projectile,    // 날아가 처음 닿은 적 (Pierce 면 관통)
        Pierce,        // 즉시 일직선 관통 (Range, 폭 = Radius)
        Cone,          // 전방 부채꼴 (Radius, Angle)
        Dash,          // 대상에게 돌진 (Pierce 면 경로 피해, Radius 면 도착 범위)
        Leap,          // 대상에게 도약 (Radius 면 착지 범위)
        AllEnemies,    // 모든 적
        GrabSlam,      // 글러트니 에피타이저: 잡은 대상을 주변 랜덤 위치에 내려찍는다
        RandomEnemies, // 무작위 적 Shots 번
        AllyLowest,    // 체력 비율이 가장 낮은 아군 (AlsoSelf 면 자신도)
        AllyAll,       // 아군 전체
        Self,          // 자신
        Zone,          // 장판 (대상 위치 또는 ZoneAtSelf)
        Wall,          // 아군과 적 사이에 벽
    }

    public enum TargetPick { Current, Farthest, LowestHp, HighestMaxHp, HighestAtk, HighestMag, HighestHp, Marked }
    public enum ShotStyle { Arc, Orb, Bullet, Arrow, Card, Shard, Potion, Spark }

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

        // ── 컨셉 스킬 (모양 · 대상 · 효과 · 이펙트). 크기 · 시간은 [임시] ──
        public SkillShape Shape;
        public TargetPick Pick;
        public FieldEffectKind Fx;          // 주 이펙트
        public FieldEffectKind Fx2;         // 보조 이펙트 (적중 · 도착 지점)
        public float FxDelay2;              // 보조 이펙트 지연 (초)
        public float FxTime;                // 지속형 이펙트 시간 (보호막 · 장판 · 벽 · 십자가 · 심판 준비)
        public Color FxColor = Color.white;
        public ShotStyle Shot;
        public int Shots = 1;               // 투사체 수 · 무작위 타격 수
        public bool Pierce;
        public float ExplodeRadius;         // 투사체 적중 시 폭발 범위
        public float Angle = 120f;          // 부채꼴 각도
        public float Knock;                 // + 밀쳐내기 / - 끌어당기기 (유닛)
        public bool PullToSelf;             // 시전자 앞까지 끌어온다
        public float Stun;                  // 기절 (초)
        public bool Roll;                   // "A ~ B" (멜): 기절 · 대상 수 · 범위를 매번 무작위
        public float Root;                  // 시전자 이동 불가 (초)
        public bool Marks;                  // 표식 (펜리르, 1개만)
        public bool NeedsAllyStatus;        // 상태이상 걸린 아군이 있을 때만
        public bool AlsoSelf;               // 아군 대상 스킬이 자신에게도
        public bool ResetShortestCd;        // 남은 쿨타임이 가장 짧은 다른 스킬 초기화
        public bool DashReturn;             // 돌진 후 제자리로 복귀
        public bool DashBehind;             // 대상 뒤까지 돌진
        public bool ZoneAtSelf;
        public string FollowUp;             // 이어서 자동으로 나가는 스킬 ID
        public float FollowDelay;
        public bool Quiet;                  // 로그에 남기지 않는 보조 스킬 (FollowUp)
        public bool Pistol;                 // 총잡이 특수탄: 장전 중 사용 불가
        public bool CollideStun;            // 밀려난 적이 적 · 벽에 부딪히면 작은 파동 + 기절
        public bool Fx2AtFront;             // 보조 이펙트를 시전자 앞에 (천멸승화각 불기둥)
        public bool CrossLaser;             // 십자가 폭발이 맵 전체를 꿰뚫는 레이저 (역십자가)
        public bool WolvesOnly;             // 아군 늑대만 (몰이 사냥)
        public float Hunt;                  // 늑대가 표식 대상을 우선하는 시간
        public string CounterSkill;         // 자세 중 피격 시 나가는 반격 (카운터 슬래시)
        public string Next;                 // 사용 후 이 스킬로 바뀜 (요월 암영 번갈아)
        public string FireArmVariant;       // 화염의 팔 보유 시 대신 나가는 스킬 (폭멸파쇄권 → 화권)
        public bool Erosion;                // 장착 중: 피해를 줄 때 어둠 잠식 부여 (어둠 거두기)
        public int KnockLevel;              // 적중 넉백 단계 1~5 (0 = 없음, 사용자 2026-11-26)
        public bool KnockSkipPrimary;       // 주대상은 넉백 제외 (나머지만). 방향은 주대상에서 바깥쪽
        public int KnockLevelOthers;        // 0 이 아니면 주대상 외에는 이 단계 (화권 화염 파도)
        public bool Channel;                // 후속 스킬까지 준비 동작 (그동안 다른 행동 안 함)
        // 동작 세부 (ConceptCombat.cs)
        public bool Armor;                  // 사용 중 슈퍼아머: 넉백 · 끌어당김 안 됨. 기절은 통하고 시전을 끊는다
        public float Lunge;                 // 앞으로 살짝 이동한 뒤 발동 (거리)
        public float Approach;              // 이 거리 안의 대상에게 돌진한 뒤 발동
        public float CastTime;              // 발동 전 대기 · 시전 시간
        public bool Aim;                    // 시전 중 조준선 이펙트
        public float Creep;                 // 발동 후 이 시간 동안 대상에게 천천히 이동
        public float ToughUp, ToughUpLowHp; // 자신 강인함 증가 (5초)
        public float ToughDown;             // 대상 강인함 감소 (5초)
        public float SelfDamagePct;         // 사용 후 자신의 최대 체력 비례 피해 (체력 1 까지만)
        public FieldEffectKind CastFx;      // 시전(대기) 중에 보이는 이펙트
        public float Launch;                // 적중한 적을 잠깐 띄우는 시간
        public float HangTime;              // 도약: 공중에 머무는 시간 (높이 뜬다)
        public float OffMapTime;            // 맵에서 이탈하는 시간 (도약: 사라졌다가 대상 위치에 낙하)
        public bool HideWhileUsing;         // 사용 중 맵 이탈 (멸령부)
        public float BarrageTime;           // 이 시간 동안 Hits 번 나눠 때린다
        public bool BarrageCone;            // 연타가 전방 부채꼴 전체를 때린다
        public bool Combo;                  // 파이어 블레이드: 큰 베기 → 대상마다 연속 베기 → 각자 폭발
        public bool NoDamage;               // 피해 없이 효과만 (심판의 일격 제압)
        public bool ImpactCone;             // 도착 지점에 부채꼴 충격파
        public bool Teleport;               // 돌진 대신 순간이동 (일섬). 경로 피해는 그대로
        public float DashSpeedMul;          // 0 이 아니면 돌진 속도 배율
        public float ImpactFrame;           // 적중 순간 필드 정지 + 색 반전 (초, 실제 시간)
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
        /// <summary>출전 가능. 스킬 컨셉이 있는 캐릭터만 (사용자 2026-11-26).</summary>
        public bool Enabled;

        // ── 컨셉 연출 · 규칙 (ConceptCharacters.cs 가 채운다) ──
        public BasicKind Basic;
        public Color BasicColor = Color.white;
        public int BasicKnock = FieldConst.MeleeBasicKnockLevel;   // 기본 공격 넉백 단계 (캐릭터별, ConceptCharacters)
        public FxTheme Theme;
        public string[] Passives = new string[0];   // 고유 패시브 3 (장착 1)
        public bool Gunner;                         // 총잡이: 기본탄 장탄 · 장전
        public bool Wolf;                           // 늑대 태그
        public bool Eclipse;                        // 이클립스 소속 (악신의 눈 재단)
        public bool FireArmTrait;                   // 화염의 팔 특성 (점화)
        public bool ShowWeapons;                    // 무기 상시 표시 (벤데타 권총 · 케이스)
        public bool IsRangedBasic => FieldData.IsRangedBasic(Basic);
    }

    public sealed class FieldEnemyDef
    {
        public string Id;                  // GameData.Enemy 의 ID 와 같다 (스탯 · 보상은 거기서)
        public Strategy Strategy;
        public float Mov;
        public float Range;
        public string[] Skills = new string[0];
        public bool Immortal;              // 체력 무한 (테스트 더미). 피해는 기록하지만 HP 가 줄지 않는다.
        public BasicKind Basic = BasicKind.None;   // 기본 공격 이펙트 (None 이면 이펙트 없음)
        public Color BasicColor = Color.white;
        public int BasicKnock = -1;                // 기본 공격 넉백 단계 (-1 = 근거리 1 · 원거리 0 기본값)
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
        public const float ProjectileHitRadius = 0.6f; // 투사체 판정 반지름 [임시]
        public const float DashSpeed = 18f;            // 돌진 · 도약 속도 (유닛/초) [임시]
        public const float DashStopDist = 1f;          // 대상 앞 이 거리에서 멈춤
        public const float PushTime = 0.25f;            // 밀쳐내기 · 끌어당기기에 걸리는 시간

        /// <summary>넉백 단계 1~5 (사용자 2026-11-26). 단계별 밀려나는 거리(유닛)는 [임시]. 0 = 넉백 없음.</summary>
        public static readonly float[] KnockDistance = { 0f, 0.5f, 1.0f, 1.6f, 2.4f, 3.2f };
        /// <summary>넉백 뒤 경직(초): 밀려난 다음 이만큼 더 이동 불가, 공격은 가능 [임시].</summary>
        public static readonly float[] KnockStagger = { 0f, 0f, 0.1f, 0.2f, 0.35f, 0.5f };
        public const float StaggerImmuneTime = 0.5f;   // 경직이 끝난 뒤 이 시간 동안은 다시 경직되지 않는다 (밀리기만)
        public const int ShakeKnockLevel = 4;          // 이 단계 이상이면 화면 흔들림
        public const int MaxKnockLevel = 5;
        // 기본 공격 넉백 기본값 (캐릭터별 값이 없을 때: 더미 · 일반 적). 캐릭터별 값은 ConceptCharacters (사용자 2026-11-26)
        public const int MeleeBasicKnockLevel = 1;
        public const int RangedBasicKnockLevel = 0;
        public static float KnockOf(int level) => KnockDistance[Mathf.Clamp(level, 0, MaxKnockLevel)];
        public const float ShotInterval = 0.15f;       // 여러 발 투사체 간격
        public const float PierceWidth = 0.7f;         // 일직선 판정 폭 (Radius 가 0 일 때)
        public const float LobTime = 0.35f;            // 포션 투척 비행 시간 (이펙트)
        public const float WallDist = 2.5f;            // 수은 장벽: 시전자 앞 거리             // 테스트 단계 출전 인원 (사용자 2026-11-26). 기획 최대 7 은 추후
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
            new FieldSkillDef { Id = "stium_spin", KnockLevel = 3, Name = "회전베기", Tag = SkillTag.Attack, Effect = SkillEffect.Spin, FxColor = SkillColor.Dark,
                Target = TargetRule.EnemyArea, Radius = 2f, Cooldown = 6f, Coef = 1.0f, MagicBase = 80f, MagicCoef = 0.5f,
                Decided = true, Desc = "전방위: 공격력 100% 물리 + (80 + 마력 50%) 마법" },
            new FieldSkillDef { Id = "stium_smash", KnockLevel = 3, Name = "강격검", Tag = SkillTag.Attack, Effect = SkillEffect.Smash, FxColor = SkillColor.Dark,
                Target = TargetRule.Enemy, Range = 1.8f, Radius = 1.2f, Cooldown = 12f, Coef = 1.5f, MagicBase = 90f, MagicCoef = 0.6f,
                Decided = true, Desc = "대상: 공격력 150% 물리 + (90 + 마력 60%) 마법" },
            new FieldSkillDef { Id = "stium_darkwave", KnockLevel = 1, Name = "어둠의 검기", Tag = SkillTag.Attack, Effect = SkillEffect.DarkWave, FxColor = SkillColor.Dark,
                Target = TargetRule.Enemy, Range = 8f, Cooldown = 10f, MagicBase = 60f, MagicCoef = 0.75f,
                Decided = true, Desc = "검기가 처음 적중한 적에게 (60 + 마력 75%) 마법" },
            new FieldSkillDef { Id = "stium_pulse", KnockLevel = 4, Name = "어둠의 파동", Tag = SkillTag.Attack, Effect = SkillEffect.Pulse, FxColor = SkillColor.Dark,
                Target = TargetRule.EnemyArea, Radius = 2.5f, Cooldown = 11f, MagicBase = 80f, MagicCoef = 0.6f,
                Decided = true, Desc = "근처: (80 + 마력 60%) 마법 (넉백 4)" },
            new FieldSkillDef { Id = "stium_grasp", Name = "어둠의 손아귀", Tag = SkillTag.Attack, Effect = SkillEffect.Grasp, FxColor = SkillColor.Dark,
                Target = TargetRule.EnemyArea, Radius = 4f, Cooldown = 10f,
                Shape = SkillShape.Around, Fx = FieldEffectKind.Grasp, FollowUp = "stium_grasp_pull", FollowDelay = 0.35f,
                Desc = "어둠을 퍼트린 뒤 중간 범위(반지름 4, 임시)를 끌어당김 (수치 미정)" },
            new FieldSkillDef { Id = "stium_harvest", Name = "어둠 거두기", Tag = SkillTag.Attack, Effect = SkillEffect.Harvest, FxColor = SkillColor.Dark,
                Target = TargetRule.EnemyArea, Radius = 6f, Cooldown = 10f, Erosion = true,
                Desc = "장착 중 피해 시 어둠 잠식 1 (최대 10, 저항력 -1). 사용 시 잠식 있는 적만: 잠식 1당 최대 체력 1% 고정 피해, 피해의 50% 회복" },
        };

        // ── 패시브 [임시] ──
        static readonly FieldPassiveDef[] PassiveTable =
        {
            new FieldPassiveDef { Id = "sturdy", Name = "단단한 몸", Tag = SkillTag.Defense, DefMul = 1.2f, Desc = "DEF +20%" },
            new FieldPassiveDef { Id = "keen_eye", Name = "날카로운 눈", Tag = SkillTag.Attack, CritAdd = 0.1f, Desc = "CRIT +10%p" },
            new FieldPassiveDef { Id = "regen", Name = "재생", Tag = SkillTag.Heal, RegenPct = 0.01f, Desc = "초당 최대 HP 1% 회복" },
            new FieldPassiveDef { Id = "swift", Name = "질주", Tag = SkillTag.Support, MovMul = 1.15f, Desc = "MOV +15%" },
        };

        static FieldData() => ConceptCharacters.Apply(Roster);

        // ── 캐릭터 [임시] ──  이름 · 유형 · 역할은 사용자 확정, 스탯 · 스킬 구성은 자리 표시
        public static readonly CharacterDef[] Roster =
        {
            // 스티움: 기준 스탯 체력 1500 · 공격력 30 · 마력 30, 기본 공격 0.6회/초 (= SPD 6) — 사용자 확정 2026-11-25.
            // 이펙트 확인용으로 고유 액티브 6개를 모두 장착 [임시] (장착 3개 규칙 · 스킬 선택 화면은 추후). 패시브 3종은 미구현.
            new CharacterDef { Id = "stium", Name = "스티움 카터렉", Kind = "근거리 딜탱, 탱커", BaseRole = Role.Tank,
                DefaultStrategy = Strategy.Charge, Base = new Stats(1500, 0, 30, 0, 6f, 0.05f, 1.5f) { MAG = 30 }, Mov = 3.0f, Range = 1.5f,
                Skills = new[] { "stium_spin", "stium_smash", "stium_darkwave", "stium_pulse", "stium_grasp", "stium_harvest" },
                Passive = "sturdy", NumbersSet = true, Enabled = true },
            new CharacterDef { Id = "ern", Name = "에른 스트레이", Kind = "근거리 딜탱", BaseRole = Role.Dealer,
                DefaultStrategy = Strategy.Flank, Base = new Stats(110, 0, 11, 4, 10f, 0.10f, 1.5f), Mov = 3.5f, Range = 1.5f,
                Skills = new[] { "ern_fire_slash", "ern_fire_stream", "ern_fire_buster", "ern_fire_execution", "ern_fire_blade", "ern_fireball" },
                Passive = "keen_eye", Enabled = true },
            new CharacterDef { Id = "mel", Name = "멜 발렌티나", Kind = "서포터, 원거리 딜러", BaseRole = Role.Supporter,
                DefaultStrategy = Strategy.Kite, Base = new Stats(80, 0, 9, 2, 10f, 0.05f, 1.5f), Mov = 3.0f, Range = 7f,
                Skills = new[] { "mel_thunder", "mel_fire_buster", "mel_coin_toss", "mel_divine_shield", "mel_railgun", "mel_absolute_zero" },
                Passive = "swift", Enabled = true },
            new CharacterDef { Id = "vendetta", Name = "벤데타", Kind = "원거리 딜러", BaseRole = Role.Dealer,
                DefaultStrategy = Strategy.Kite, Base = new Stats(75, 0, 12, 2, 10f, 0.10f, 1.6f), Mov = 3.0f, Range = 8f,
                Skills = new[] { "vd_incendiary", "vd_freeze", "vd_pierce", "vd_pilebunker", "vd_missiles", "vd_sniper" },
                Passive = "keen_eye", Enabled = true },
            new CharacterDef { Id = "paracelsus", Name = "파라켈수스", Kind = "서포터", BaseRole = Role.Supporter,
                DefaultStrategy = Strategy.Escort, Base = new Stats(85, 0, 6, 3, 9f, 0.05f, 1.5f), Mov = 3.0f, Range = 6f,
                Skills = new[] { "pc_coagulant", "pc_purify", "pc_corrode", "pc_silver_arrow", "pc_elixir", "pc_mercury_wall" },
                Passive = "regen", Enabled = true },

            // 나머지 43명: 이름 · 유형 · 기본 역할 · 기본 전략은 기획서 5장 표 그대로.
            // 스탯 · 스킬은 첫 번째 유형의 틀(Template)로 채운 자리 표시 [임시] — 고유 스킬 구현 전 구조 테스트용.
            T("c06", "메일 보레아", "근거리 딜탱", Role.Dealer, Strategy.Charge, Tpl.Bruiser, new[] { "mail_shoulder", "mail_stomp", "mail_smash", "mail_harden", "mail_slash_blast", "mail_overdrive" }),
            T("c07", "모모로이 세이린", "탱커, 서포터", Role.Tank, Strategy.Charge, Tpl.Tank),
            T("c08", "투견", "근거리 딜탱", Role.Dealer, Strategy.Charge, Tpl.Bruiser),
            T("c09", "마리스 레지나", "암살자", Role.Dealer, Strategy.Flank, Tpl.Assassin),
            T("c10", "레일 루카스", "근거리 딜러", Role.Dealer, Strategy.Flank, Tpl.Melee),
            T("c11", "베들렌 메릴", "근거리 딜탱", Role.Dealer, Strategy.Charge, Tpl.Bruiser),
            T("c12", "릭 엔더빌", "근거리 딜러", Role.Dealer, Strategy.Flank, Tpl.Melee),
            T("c13", "아그니", "근거리 딜러", Role.Dealer, Strategy.Charge, Tpl.Melee, new[] { "agni_kick", "agni_flurry", "agni_rising", "agni_meteor", "agni_crusher", "agni_knee" }),
            T("c14", "노와", "암살자", Role.Dealer, Strategy.Flank, Tpl.Assassin),
            T("c15", "슈진 잔엔", "근거리 딜러", Role.Dealer, Strategy.Charge, Tpl.Melee),
            T("c16", "청란", "근거리 딜탱", Role.Dealer, Strategy.Charge, Tpl.Bruiser),
            T("c17", "한니발 멜카르티드", "암살자", Role.Dealer, Strategy.Flank, Tpl.Assassin),
            T("c18", "이게르투스", "탱커", Role.Tank, Strategy.Charge, Tpl.Tank),
            T("c19", "요월", "근거리 딜러", Role.Dealer, Strategy.Charge, Tpl.Melee, new[] { "yw_moonlight", "yw_moonflash", "yw_fullmoon", "yw_moonfall", "yw_eclipse_cut", "yw_annular" }),
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
            T("c32", "라바다", "근거리 딜탱, 서포터", Role.Dealer, Strategy.Escort, Tpl.Bruiser, new[] { "lv_light_pierce", "lv_blessing", "lv_silence", "lv_sanctuary", "lv_bless_judge", "lv_judgement" }),
            T("c33", "엘 카벤", "근거리 딜탱, 서포터", Role.Dealer, Strategy.Escort, Tpl.Bruiser),
            T("c34", "사우윈 키르윈", "탱커", Role.Tank, Strategy.Charge, Tpl.Tank),
            T("c35", "이믈록 사비아", "원거리 딜러", Role.Dealer, Strategy.Kite, Tpl.Ranged),
            T("c36", "발타너 트래헌", "근거리 딜러", Role.Dealer, Strategy.Flank, Tpl.Melee),
            T("c37", "루나사 키아라", "원거리 딜러, 서포터", Role.Dealer, Strategy.Kite, Tpl.Ranged),
            T("c38", "레온하르트", "근거리 딜탱, 탱커, 서포터", Role.Dealer, Strategy.Charge, Tpl.Bruiser, new[] { "lh_lion_crash", "lh_roar", "lh_kings_leap", "lh_buster_slash", "lh_counter_slash", "lh_sky_splitter" }),
            T("c39", "드로미", "근거리 딜탱", Role.Dealer, Strategy.Charge, Tpl.Bruiser),
            T("c40", "펜리르", "근거리 딜탱", Role.Dealer, Strategy.Charge, Tpl.Bruiser, new[] { "fr_hunt_leap", "fr_ambush", "fr_chain", "fr_eclipse_jaws", "fr_pack_hunt", "fr_ground_slam" }),
            T("c41", "사하라", "근거리 딜탱, 서포터", Role.Dealer, Strategy.Escort, Tpl.Bruiser, new[] { "sh_flash", "sh_seal", "sh_suppress", "sh_dark_cut", "sh_annihilate", "sh_ward" }),
            T("c42", "갈 빌랜드", "탱커, 서포터", Role.Tank, Strategy.Charge, Tpl.Tank),
            T("c43", "니그레도", "원거리 딜러", Role.Dealer, Strategy.Kite, Tpl.Ranged, new[] { "ng_black_shot", "ng_black_solution", "ng_black_sediment", "ng_black_chain", "ng_decompose", "ng_black_guard" }),
            T("c44", "알베도", "암살자", Role.Dealer, Strategy.Flank, Tpl.Assassin),
            T("c45", "치트리니타스", "탱커", Role.Tank, Strategy.Charge, Tpl.Tank),
            T("c46", "비리디타스", "서포터", Role.Supporter, Strategy.Escort, Tpl.Support),
            T("c47", "루베도", "원거리 딜러", Role.Dealer, Strategy.Kite, Tpl.Ranged),
            T("c48", "가니악 벤데스트라", "근거리 딜탱", Role.Dealer, Strategy.Charge, Tpl.Bruiser, new[] { "gn_fang", "gn_wave", "gn_grasp", "gn_abyss_dance", "gn_inverted_cross", "gn_erosion" }),
        };

        /// <summary>자리 표시 캐릭터의 스탯 · 스킬 틀 (첫 번째 유형 기준) [임시].</summary>
        enum Tpl { Bruiser, Melee, Tank, Assassin, Ranged, Support }

        // 정적 필드 초기화 순서에 기대지 않도록 값은 메서드 안에서 만든다.
        /// <param name="concept">스킬 컨셉 액티브 (있으면 출전 가능, 없으면 비활성)</param>
        static CharacterDef T(string id, string name, string kind, Role role, Strategy strategy, Tpl tpl, string[] concept = null)
        {
            var c = T0(id, name, kind, role, strategy, tpl);
            if (concept != null) { c.Skills = concept; c.Enabled = true; }
            return c;
        }

        static CharacterDef T0(string id, string name, string kind, Role role, Strategy strategy, Tpl tpl)
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
            // 기본 공격: 근접 더미 횡베기 · 원거리 더미 파티클 탄 (사용자 2026-11-26). 공격력 0 이라 피해는 0.
            new FieldEnemyDef { Id = "test_dummy_melee", Strategy = Strategy.Charge, Mov = 2.5f, Range = 1.5f, Immortal = true,
                Basic = BasicKind.HSlash, BasicColor = new Color(1f, 0.45f, 0.35f) },
            new FieldEnemyDef { Id = "test_dummy_ranged", Strategy = Strategy.Kite, Mov = 2.5f, Range = 7f, Immortal = true,
                Basic = BasicKind.Bolt, BasicColor = new Color(1f, 0.55f, 0.3f) },
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
                foreach (var s in ConceptSkills.All) skillById[s.Id] = s;
                foreach (var s in skillById.Values) ConceptCombat.Apply(s);
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
                foreach (var p in ConceptCharacters.Passives) passiveById[p.Id] = p;
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

        /// <summary>캐릭터 기본 구성으로 만든 편성 한 칸 (액티브 앞 3개 · 패시브 첫 번째).</summary>
        public static PartySlot DefaultSlot(string characterId)
        {
            var c = Character(characterId);
            int n = Mathf.Min(MaxEquipActives, c.Skills.Length);
            var skills = new string[n];
            System.Array.Copy(c.Skills, skills, n);
            return new PartySlot
            {
                Def = c,
                Strategy = c.DefaultStrategy,
                Skills = skills,
                Passive = c.Passives.Length > 0 ? c.Passives[0] : c.Passive,
            };
        }

        public const int MaxEquipActives = 3;     // 장착: 액티브 3 + 패시브 1

        /// <summary>선택 화면의 전술 4종 (사용자 2026-11-26).</summary>
        public static readonly Strategy[] PlayerStrategies = { Strategy.Charge, Strategy.Flank, Strategy.Kite, Strategy.Backline };

        // ── 기본 공격 투사체 (원거리 기본 공격) ──
        static readonly Dictionary<BasicKind, FieldSkillDef> basicShots = new Dictionary<BasicKind, FieldSkillDef>();

        public static bool IsRangedBasic(BasicKind k) =>
            k == BasicKind.IceShard || k == BasicKind.Pistol || k == BasicKind.Potion || k == BasicKind.DarkOrb || k == BasicKind.Bolt;

        /// <summary>원거리 기본 공격용 투사체 정의 (공격력 100% 물리). 종류별로 하나.</summary>
        public static FieldSkillDef BasicShot(BasicKind kind, Color color, float range)
        {
            if (basicShots.TryGetValue(kind, out var s)) return s;
            ShotStyle style = kind == BasicKind.IceShard ? ShotStyle.Shard
                            : kind == BasicKind.Pistol ? ShotStyle.Bullet
                            : kind == BasicKind.Potion ? ShotStyle.Potion
                            : kind == BasicKind.Bolt ? ShotStyle.Spark : ShotStyle.Orb;
            s = new FieldSkillDef { Id = "basic_" + kind, Name = "공격", Shape = SkillShape.Projectile, Coef = 1f,
                                    Range = range + 1f, Shot = style, FxColor = color, Fx2 = FieldEffectKind.Burst, Radius = 0.45f, Quiet = true,
                                    KnockLevel = 0 };   // 넉백은 쏜 유닛의 BasicKnock 으로 (FieldProjectile.BasicKnock)
            basicShots[kind] = s;
            return s;
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
                case Strategy.Backline: return Role.Dealer;
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
                case Strategy.Flank: return "후방 침입";
                case Strategy.Kite: return "도주 · 견제";
                case Strategy.Escort: return "호위";
                case Strategy.Backline: return "후방 우선 공격";
                default: return "진형 유지";
            }
        }
    }
}
