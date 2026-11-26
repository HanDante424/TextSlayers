using System.Collections.Generic;
using UnityEngine;

namespace TextSlayers
{
    public enum FieldPhase { Running, WaveClear, Won, Lost }
    public enum FieldEventKind { Hit, Heal, Skill, Death, Wave, Info }

    /// <summary>화면 연출용 이벤트 (피해 숫자 · 스킬명 · 로그). 시뮬레이션 결과에는 영향 없음.</summary>
    public struct FieldEvent
    {
        public float Time;
        public FieldEventKind Kind;
        public FieldUnit Source;
        public FieldUnit Target;
        public int Amount;
        public bool Crit;
        public string Text;
        public bool Quiet;        // 범위 스킬의 개별 타격 (로그에는 요약 한 줄만)
    }

    public enum FieldEffectKind
    {
        None,
        Spin, Smash, Burst, Pulse, Grasp, Harvest,         // 스티움에서 시작 (회전 · 파괴 · 폭발 · 퍼지는 원 · 모이는 원 · 끌려오는 파티클)
        Slash, Multi, Trail, Leap, Pillar, Heal, Shield, Buff, Rain, Zone, Wall, Cross, Bite, Links, Charge, Lob, Roar, Beam, Cone,
        Blink,      // 순간이동 잔상 + 참격 (멸령부)
        MapSlash,   // 맵 전체를 가르는 참격 (오의 암영 - 절)
        Swing,      // 기본 공격 휘두르기 (Count = BasicKind)
        Aim,        // 저격 조준선 (두 선이 모인다)
        BladeSlam,  // 직사각형 대검을 내려찍는다
        BladeRaise, // 대검을 수직으로 들어 올림 (시전)
        BladeSwing, // 직사각형 대검으로 크게 베기
        BigSlash,   // 큰 베기 한 번
        Whirl,      // 자신 둘레를 도는 연속 참격 (화염)
        Coin,       // 튀어 오르는 동전
        Freeze,     // 필드가 얼어붙음 + 적 얼음
        Grab,       // 잡기 (대상을 원으로 감쌈)
        Missiles,   // 케이스에서 솟아 낙하하는 미사일
        Punches,    // 연속 주먹
        SlantPillar,// 비스듬한 불기둥
        CrossHold,  // 십자가를 들어 올림
        CrossSlam,  // 십자가 내려찍기
        CrossFall,  // 대상 위에서 십자가가 떨어짐 (Duration 뒤 착탄)
        Maw,        // 거대한 이빨 입이 삼킨다
        ForwardSlashes, // 전방 연속 베기
        FanWave,    // 부채꼴 충격파
        FireLine,   // 맵을 꿰뚫는 직선 화염
        LeapOff,    // 맵 위로 솟구침
        Landing,    // 낙하 착지 (그림자 → 충격)
    }

    /// <summary>
    /// 화면 연출 기록 (스킬 이펙트). 전투 결과에는 영향 없다. 언제 · 어디서 일어났는지만 기록하고,
    /// 얼마나 오래 · 어떻게 그릴지는 GameView 가 정한다.
    /// </summary>
    public sealed class FieldEffect
    {
        public FieldEffectKind Kind;
        public float Start;          // FieldBattle.Time 기준
        public Vector2 Origin;       // 시전 시점 위치 (필드 좌표)
        public Vector2 Dir;          // 시전 방향 (단위 벡터)
        public float Radius;
        public FieldUnit Source;     // 끌려오는 연출의 도착점 · 보호막을 두른 대상 (현재 위치를 따라간다)
        public Vector2[] Points;     // 여러 지점 (거두기 · 폭격 · 사슬 · 모든 적)
        public Vector2 End;          // 선 · 돌진 · 투척의 끝점
        public Color Color = Color.white;
        public float Delay;          // 이 시간이 지난 뒤 나타난다
        public float Duration;       // 지속형 (보호막 · 장판 · 벽 · 십자가 · 심판 준비). 0 이면 화면 기본값
        public int Count;            // 연타 수
        public FieldUnit Target;     // 대상에 붙어 따라다니는 이펙트 (침묵 선언 십자가)
        public FieldUnit[] Units;    // 대상들을 잇는 이펙트 (흑색 사슬)
        public bool Spawned;         // 화면이 파티클을 이미 뿌렸는가 (화면 전용 상태)
        public FxTheme Theme;        // 시전자의 파티클 테마
    }

    /// <summary>날아가는 투사체 (어둠의 검기). 처음 닿은 적에게 피해.</summary>
    public sealed class FieldProjectile
    {
        public Vector2 Pos;
        public Vector2 Dir;
        public float Left;           // 남은 비행 거리
        public FieldUnit Source;
        public FieldSkillDef Skill;
        public bool Done;
        public float Delay;          // 발사 전 대기 (여러 발)
        public float Traveled;       // 날아간 거리 (포션 포물선용)
        public int BasicKnock;       // 기본 공격 투사체의 넉백 단계 (쏜 유닛 기준)
        public float Total;          // 전체 비행 거리
        public readonly HashSet<FieldUnit> Hit = new HashSet<FieldUnit>(); // 관통 시 같은 적 중복 방지
    }

    /// <summary>예약된 후속 스킬 (남은 시간 · 처음 스킬의 주대상).</summary>
    public sealed class FollowUpEntry
    {
        public FieldSkillDef Skill;
        public float Delay;
        public FieldUnit Primary;
    }

    /// <summary>필드 위의 유닛 한 명 (아군 또는 적).</summary>
    public sealed class FieldUnit
    {
        public readonly Combatant C;          // 스탯 · HP (피해 계산은 Damage.cs 가 이걸 읽는다)
        public readonly bool IsPlayer;
        public Strategy Strategy;             // 디버그로 전투 중 바꿀 수 있다
        public readonly Role Role;
        public readonly float Mov;
        public readonly float Range;
        public float AggroMul => Strategy == Strategy.Charge ? FieldConst.ChargeAggro
                               : Strategy == Strategy.Flank ? FieldConst.FlankAggro : 1f;
        public FieldSkillDef[] Skills;        // 월영 · 번갈아 바뀌는 스킬 때문에 교체 가능
        public readonly FieldPassiveDef Passive;
        public readonly EnemyDef EnemyBase;   // 적일 때만 (경험치 · 골드)
        public readonly float[] Cooldowns;

        public Vector2 Pos;
        public float ActionTimer;             // 0 이 되면 행동 가능
        public bool FlankDone;                // 후방 침투: 우회를 끝내고 접근 중인가
        public float FlankEdgeY;
        public float GuardTimer;
        public float BuffTimer;
        public float BuffMul = 1f;
        public float TauntTimer;
        public FieldUnit TauntTarget;
        public float RegenCarry;
        public bool RewardGiven;
        public int DamageTaken;
        public int DamageDealt;
        public bool Immortal;                 // 체력 무한 (테스트 더미)
        public float DamageMul = 1f;          // 0 이면 피해 0 (수치 미정 캐릭터, 사용자 2026-11-26)
        public Vector2 Facing = Vector2.right; // 마지막으로 노린 방향 (이펙트용)

        // 컨셉 스킬 상태 [임시]
        public bool Marked;                   // 펜리르 표식
        public float StunTimer;               // 기절: 행동 · 이동 불가
        public float RootTimer;               // 이동 불가 (행동은 가능)
        public Vector2 PushVel;               // 밀쳐내기 · 끌어당기기
        public float PushTimer;
        public bool Dashing;                  // 돌진 · 도약 중 (AI 정지)
        public Vector2 DashTo;
        public bool HasReturn;                // 돌진 후 복귀
        public Vector2 ReturnTo;
        public FieldUnit PrimaryTarget;       // 마지막 스킬의 주대상 (주대상 제외 넉백 · 후속 파동)
        public readonly List<FollowUpEntry> FollowUps = new List<FollowUpEntry>();   // 이어서 나갈 스킬들
        public float StaggerTimer;            // 넉백 경직: 이동 불가 (공격은 가능)
        public float StaggerImmuneTimer;      // 이 동안은 다시 경직되지 않음
        // 도주 · 견제
        public Vector2 KiteDir;               // 지금 도망가는 방향
        public float KiteDirTimer;            // 이 방향을 유지하는 시간 (좌우로 떠는 것 방지)
        public float BreakTimer;              // 돌파 중: 가속
        public float BreakCooldown;
        // 시전 동작 · 슈퍼아머 · 강인함
        public FieldSkillDef Casting;         // 발동 전 동작 중인 스킬 (전진 · 접근 · 시전)
        public FieldUnit CastTarget;
        public float CastWait;                // 발동까지 남은 시간
        public float LungeLeft;
        public float CastTimer;               // 발동 후 '사용 중' (다른 행동 안 함)
        public float CreepTimer;
        public float ArmorTimer;
        public float ToughUp, ToughUpTimer, ToughDown, ToughDownTimer;
        public bool SuperArmor => ArmorTimer > 0f || MoonShadow || Glutton;   // 월영 · 글러트니: 상시 슈퍼아머
        public float Toughness => Mathf.Clamp((ToughUpTimer > 0f ? ToughUp : 0f) - (ToughDownTimer > 0f ? ToughDown : 0f), 0f, 100f);
        public int SpecialAmmo;               // 장전된 특수탄 (총잡이): 다음 기본 공격이 특수탄
        public FieldSkillDef SpecialSkill;
        // 띄움 · 맵 이탈 · 잡힘 · 분산 연타
        public float AirTimer, AirTotal, AirHeight;     // 공중 (그림에서 위로 띄움). 띄워진 적은 행동 불가
        public float OffMapTimer;
        public Vector2 LandPos;
        public float HeldTimer;                         // 잡힘 (행동 불가, 원으로 감쌈)
        public FieldUnit Grabbed;                       // 내가 잡은 대상
        public FieldSkillDef Barrage;
        public int BarrageLeft;
        public float BarrageInterval, BarrageNext;
        public float DashSpeed = FieldConst.DashSpeed;
        public bool Hidden => OffMapTimer > 0f;
        public bool Targetable => Alive && !Hidden;
        public System.Action ArrivalHit;      // 돌진 · 도약: 도착하는 순간 적용할 타격
        public float ChannelTimer;            // 준비 동작 중 (심판의 일격): 다른 행동 · 이동 안 함

        public CharacterDef Def;              // 아군일 때만
        public BasicKind Basic = BasicKind.None;   // 기본 공격 이펙트 (아군 · 적 공통)
        public Color BasicColor = Color.white;
        public int BasicKnock;                     // 기본 공격 넉백 단계
        public FxTheme Theme;
        public float LastHitTime = -99f;      // 피격 번쩍임 (화면)
        public int Ammo;                      // 총잡이 기본탄
        public float ReloadTimer;             // 장전 중 (기본탄 · 특수탄 불가)
        public int DarkErosion;               // 어둠 잠식 (스티움, 최대 10, 저항력 -1 / 1)
        public bool AppliesErosion;           // 어둠 거두기 장착
        public float CounterTimer;            // 카운터 자세 (아무것도 안 함, 피격 시 반격)
        public FieldSkillDef CounterSkill;
        public float HuntTimer;               // 몰이 사냥: 표식 대상 우선
        public float Ignite;                  // 점화 (화염의 팔)
        public bool FireArm;
        public float FireArmBurn;             // 화염의 팔 자해 누적 시간 (1초마다)
        public bool Glutton;                  // 멜 글러트니 변신
        public bool MoonShadowPassive;        // 요월 월영 장착
        public bool MoonShadow;               // 월영 상태 (악신의 눈)
        public int ShadowTrace;               // 그림자 흔적 (최대 15)
        public FieldSkillDef[] NormalSkills;  // 월영 전 장착 스킬 (웨이브 끝에 복구)
        public bool PushCollide;              // 밀려나는 중 충돌하면 기절

        public FieldUnit(Combatant c, bool isPlayer, Strategy strategy, Role role, float mov, float range,
                         FieldSkillDef[] skills, FieldPassiveDef passive, EnemyDef enemyBase)
        {
            C = c;
            IsPlayer = isPlayer;
            Strategy = strategy;
            Role = role;
            Passive = passive;
            Mov = mov * (passive != null ? passive.MovMul : 1f);
            Range = range;
            Skills = skills;
            EnemyBase = enemyBase;
            Cooldowns = new float[skills.Length];
        }

        public string Name => C.Name;
        public bool Alive => C.Alive;
        public float HpRatio => C.HpRatio;
    }

    /// <summary>
    /// 스테이지 1회 실시간 필드 전투 (기획 v0.2 7~8장). MonoBehaviour 가 아니며 고정 스텝(FieldConst.Dt)으로 진행한다.
    /// 화면 없이 FieldSim 으로 돌릴 수 있다. 피해 · 회복 계산은 Damage.cs 만 쓴다.
    /// </summary>
    /// <summary>디버그 맵 더미 한 명 (종류 + 전술).</summary>
    public sealed class EnemyEntry
    {
        public string Id;
        public Strategy Strategy;
    }

    /// <summary>디버그 맵 적 구성 (사용자 2026-11-26): 더미마다 종류 · 전술을 따로 정한다. 첫 웨이브를 이 목록으로 바꾼다.</summary>
    public sealed class EnemySetup
    {
        public readonly List<EnemyEntry> Entries = new List<EnemyEntry>();

        public void Add(string id, Strategy strategy, int count = 1)
        {
            for (int i = 0; i < count; i++) Entries.Add(new EnemyEntry { Id = id, Strategy = strategy });
        }

        public int Total => Entries.Count;
    }

    public sealed class FieldBattle
    {
        public readonly FieldStageDef Stage;
        public readonly List<FieldUnit> Players = new List<FieldUnit>();
        public readonly List<FieldUnit> Enemies = new List<FieldUnit>();
        public readonly List<FieldEvent> Events = new List<FieldEvent>(); // recordEvents 가 true 일 때만 쌓인다. 화면이 비운다.
        public readonly List<FieldEffect> Effects = new List<FieldEffect>(); // recordEvents 가 true 일 때만. EffectMaxAge 지나면 지운다.
        public readonly List<FieldProjectile> Projectiles = new List<FieldProjectile>();
        public const float EffectMaxAge = 8f;  // 가장 긴 지속형 이펙트(장판 5초 + 여유)보다 길게

        public FieldPhase Phase { get; private set; }
        public int WaveIndex { get; private set; }
        public int WaveCount => Stage.Waves.Length;
        public float Time { get; private set; }
        public bool Retreated { get; private set; }
        public bool TimedOut { get; private set; }
        public int EarnedExp { get; private set; }
        public int EarnedGold { get; private set; }
        public bool IsOver => Phase == FieldPhase.Won || Phase == FieldPhase.Lost;

        // ── 디버그 (테스트 맵 전용, 사용자 2026-11-26) ──
        public const int MaxDebugEnemies = 30;
        /// <summary>false 면 적이 움직이지도 행동하지도 않는다 (밀려나기만 함).</summary>
        public bool EnemyAI = true;
        /// <summary>피해 집계 시작 시각. 집계 초기화 때 갱신.</summary>
        public float MeterStart { get; private set; }
        public float MeterTime => Mathf.Max(0.1f, Time - MeterStart);

        readonly System.Random rng;
        readonly bool recordEvents;
        readonly List<FieldUnit> all = new List<FieldUnit>();
        float waveTimer;
        float accumulator;

        // ── 컨셉 규칙 상수 [임시] ──
        const float GluttonRange = 1.5f;          // 글러트니: 근거리
        public const int GunnerAmmo = 6;          // 기본탄 장탄수 (벤데타 대구경 탄두 6)
        public const float ReloadTime = 2f;       // 장전 시간 (사용자 2026-11-26 임시 2초)
        const int MaxErosion = 10;
        const float ErosionDamagePerStack = 0.01f; // 어둠 거두기: 잠식 1당 최대 체력 1%
        const float HarvestHealRatio = 0.5f;
        const float AltarEye = 50f;               // 악신의 눈 (재단 게이지)
        const float AltarKill = 20f;
        const float IgniteMax = 100f, IgniteBasic = 1f, IgniteSkill = 5f, IgniteKill = 10f;
        const int ShadowTraceMax = 15;
        const float CollideStunTime = 1f;
        const float FlankEdgeMargin = 0.8f;       // 후방 침입: 가장자리에서 이만큼 안쪽
        const float FlankBehind = 1.5f;           // 대상 뒤 이만큼까지 돌아 들어감
        // 도주 · 견제 [임시]
        const int KiteDirections = 16;            // 도주 방향 후보 수
        const float KiteLook = 3f;                // 이만큼 앞을 보고 방향을 평가
        const float KiteSense = 7f;               // 이 거리 안의 적을 모두 고려
        const float KiteHold = 0.6f;              // 고른 방향 유지 시간
        const float KiteWallMargin = 1.5f;        // 벽에서 이 안이면 가장자리에 몰린 것으로 본다
        const float KiteBlockWidth = 1.1f;        // 진로에서 이 안의 적은 길을 막는 것으로 본다
        const int BreakFoes = 2;                  // 돌파 조건: 가까운 적 수
        const float BreakRadius = 2.4f;           // 돌파 때 밀어내는 범위
        const int BreakKnockLevel = 3;
        const float BreakDuration = 2f;           // 가속 지속 (사용자 2026-11-26)
        const float BreakSpeedMul = 1.5f;
        const float BreakCooldownTime = 8f;
        const float FlankWallGap = 0.3f;          // 대상 뒤 지점이 벽에서 떨어지는 거리
        const float FlankArrive = 0.6f;           // 이 안이면 우회를 끝내고 대상에게 붙는다
        const float FlankLane = 3f;               // 가장자리 길목 폭: 이 안의 상대가 많으면 반대쪽으로
        const float FlankBacklineBonus = 1000f;   // 후방 침입: 원거리 · 서포터를 먼저 (없으면 가장 뒤의 적)

        /// <summary>이클립스 재단 게이지 (이클립스 아군이 있을 때만). 50 에서 악신의 눈.</summary>
        public float Altar { get; private set; }
        public bool EyeOpen { get; private set; }
        public int EclipseCount { get; private set; }
        public bool HasAltar => EclipseCount > 0;

        /// <summary>디버그 맵 적 구성 (null 이면 스테이지 그대로).</summary>
        readonly EnemySetup enemySetup;

        public FieldBattle(FieldStageDef stage, PartySlot[] party, int level, int seed, bool recordEvents = false, EnemySetup enemies = null)
        {
            Stage = stage;
            enemySetup = enemies;
            this.recordEvents = recordEvents;
            rng = new System.Random(seed);

            for (int i = 0; i < party.Length; i++)
            {
                var slot = party[i];
                var passive = FieldData.Passive(slot.Passive);
                var stats = FieldData.ApplyPassive(FieldData.StatsAt(slot.Def, level), passive);
                var c = new Combatant(slot.Def.Name, stats, true);
                bool glutton = slot.Passive == ConceptCharacters.GluttonPassive;
                string[] equip = glutton ? ConceptCharacters.GluttonSkills : slot.Skills;
                var u = new FieldUnit(c, true, slot.Strategy,
                    FieldData.CurrentRole(slot.Def, slot.Strategy, equip, slot.Passive),
                    slot.Def.Mov, glutton ? GluttonRange : slot.Def.Range, LookupSkills(equip), passive, null);
                u.DamageMul = slot.Def.NumbersSet ? 1f : 0f;
                u.Def = slot.Def;
                u.Basic = glutton ? BasicKind.Glutton : slot.Def.Basic;
                u.BasicColor = glutton ? SkillColor.Gray : slot.Def.BasicColor;
                u.BasicKnock = glutton ? FieldConst.MeleeBasicKnockLevel : slot.Def.BasicKnock;   // 글러트니(근거리 변신)는 근거리 기본값 [해석]
                u.Glutton = glutton;
                u.Theme = glutton ? FxTheme.Gray : slot.Def.Theme;
                u.MoonShadowPassive = slot.Passive == ConceptCharacters.MoonShadowPassive;
                if (slot.Def.Gunner) u.Ammo = GunnerAmmo;
                foreach (var sk in u.Skills) if (sk.Erosion) u.AppliesErosion = true;
                if (slot.Def.Eclipse) EclipseCount++;
                float yFrac = (i + 1f) / (party.Length + 1f);
                u.Pos = new Vector2(FieldConst.PlayerStartX + (i % 2) * 1f, FieldConst.Height * yFrac);
                u.FlankEdgeY = 0f;   // 후방 침입 가장자리는 첫 이동 때 적 배치를 보고 정한다
                u.ActionTimer = Interval(u) * (float)rng.NextDouble() * 0.5f;
                Players.Add(u);
            }

            Emit(FieldEventKind.Info, null, null, 0, false, $"── {stage.Name} ──");
            Emit(FieldEventKind.Info, null, null, 0, false, stage.Intro);
            SpawnWave(0);
        }

        // ───────────── 진행 ─────────────

        /// <summary>실제 경과 시간 × 배속 만큼 고정 스텝을 진행한다 (화면에서 부른다).</summary>
        public void Update(float realDt, float speed)
        {
            // 임팩트 프레임: 필드 정지 (배속과 무관하게 실제 시간)
            if (ImpactTimer > 0f)
            {
                ImpactTimer -= realDt;
                accumulator = 0f;
                return;
            }
            accumulator += realDt * speed;
            int steps = 0;
            while (accumulator >= FieldConst.Dt && !IsOver && ImpactTimer <= 0f && steps < FieldConst.MaxStepsPerUpdate)
            {
                Step();
                accumulator -= FieldConst.Dt;
                steps++;
            }
            if (steps >= FieldConst.MaxStepsPerUpdate) accumulator = 0f; // 따라잡기 포기
        }

        /// <summary>고정 스텝 1회. 헤드리스 시뮬레이션은 이걸 직접 부른다.</summary>
        public void Step()
        {
            const float dt = FieldConst.Dt;

            if (Phase == FieldPhase.WaveClear)
            {
                waveTimer -= dt;
                if (waveTimer <= 0f) SpawnWave(WaveIndex + 1);
                return;
            }
            if (Phase != FieldPhase.Running) return;

            Time += dt;
            if (!Stage.IsTest && Time > FieldConst.TimeLimit)
            {
                TimedOut = true;
                Phase = FieldPhase.Lost;
                Emit(FieldEventKind.Info, null, null, 0, false, "시간 초과...");
                return;
            }

            for (int i = 0; i < all.Count; i++) if (all[i].Alive) TickTimers(all[i], dt);
            UpdateAltar(dt);
            for (int i = 0; i < all.Count; i++)
            {
                var u = all[i];
                if (!u.Alive || (!u.IsPlayer && !EnemyAI)) continue;
                Think(u, dt);
            }
            UpdateProjectiles(dt);
            Separate();
            CheckEnd();
            if (recordEvents) Effects.RemoveAll(e => Time - e.Start > EffectMaxAge);
        }

        /// <summary>전투 포기. 패배와 같은 보상 비율.</summary>
        public void Retreat()
        {
            if (IsOver) return;
            Retreated = true;
            Phase = FieldPhase.Lost;
            Emit(FieldEventKind.Info, null, null, 0, false, "파티가 물러났다.");
        }

        /// <summary>전투가 끝난 뒤 실제로 받는 보상. 패배 · 후퇴는 DefeatRewardRate, 메모리 없음.</summary>
        public void GetFinalRewards(out int exp, out int gold, out int memory)
        {
            if (Phase == FieldPhase.Won)
            {
                exp = EarnedExp;
                gold = EarnedGold;
                memory = Stage.ClearMemory;
            }
            else
            {
                exp = Mathf.FloorToInt(EarnedExp * GameData.DefeatRewardRate);
                gold = Mathf.FloorToInt(EarnedGold * GameData.DefeatRewardRate);
                memory = 0;
            }
        }

        // ───────────── 웨이브 ─────────────

        void SpawnWave(int index)
        {
            WaveIndex = index;
            Enemies.Clear();
            Projectiles.Clear();
            if (index == 0 && enemySetup != null)
            {
                var list = enemySetup.Entries;
                for (int i = 0; i < list.Count; i++)
                    Enemies.Add(MakeEnemy(list[i].Id, StartPos(i, list.Count), list[i].Strategy));
            }
            else
            {
                var ids = Stage.Waves[index];
                for (int i = 0; i < ids.Length; i++) Enemies.Add(MakeEnemy(ids[i], StartPos(i, ids.Length)));
            }

            all.Clear();
            all.AddRange(Players);
            all.AddRange(Enemies);
            Phase = FieldPhase.Running;
            Altar = 0f;
            EyeOpen = false;
            foreach (var u in Players) { if (u.MoonShadow) EndMoonShadow(u); u.Marked = false; u.FollowUps.Clear(); u.ChannelTimer = 0f; CancelCast(u); u.SpecialAmmo = 0; u.ArrivalHit = null; u.Dashing = false; u.OffMapTimer = 0f; u.HeldTimer = 0f; u.Grabbed = null; u.Barrage = null; u.AirTimer = 0f; }
            Emit(FieldEventKind.Wave, null, null, index + 1, false, $"[{index + 1}/{WaveCount}] 적이 나타났다!");
        }

        /// <summary>적 시작 위치: 오른쪽에 세로로 고르게, 많으면 여러 줄.</summary>
        static Vector2 StartPos(int i, int count)
        {
            const int PerColumn = 8;
            int cols = Mathf.Max(1, Mathf.CeilToInt(count / (float)PerColumn));
            int col = i % cols, row = i / cols;
            int rows = Mathf.CeilToInt(count / (float)cols);
            float yFrac = (row + 1f) / (rows + 1f);
            return new Vector2(FieldConst.EnemyStartX + col * 1.1f - (cols - 1) * 0.55f + (row % 2) * 0.3f, FieldConst.Height * yFrac);
        }

        FieldUnit MakeEnemy(string id, Vector2 pos, Strategy? strategyOverride = null)
        {
            var fe = FieldData.Enemy(id);
            var eb = GameData.Enemy(id);
            var c = new Combatant(eb.Name, eb.Stats, false, eb);
            var strategy = strategyOverride ?? fe.Strategy;
            var u = new FieldUnit(c, false, strategy, EnemyRole(strategy), fe.Mov, fe.Range,
                LookupSkills(fe.Skills), null, eb);
            u.Immortal = fe.Immortal;
            u.Basic = fe.Basic;
            u.BasicColor = fe.BasicColor;
            u.BasicKnock = fe.BasicKnock >= 0 ? fe.BasicKnock
                         : FieldData.IsRangedBasic(fe.Basic) ? FieldConst.RangedBasicKnockLevel : FieldConst.MeleeBasicKnockLevel;
            u.Pos = pos;
            ClampToField(u);
            u.ActionTimer = Interval(u) * (float)rng.NextDouble() * 0.5f;
            return u;
        }

        // ───────────── 디버그 (테스트 맵에서만 화면이 부른다) ─────────────

        /// <summary>적 1명 배치. 최대 MaxDebugEnemies. 성공하면 true.</summary>
        public bool DebugSpawn(string enemyId, Vector2 pos)
        {
            if (IsOver || Enemies.Count >= MaxDebugEnemies) return false;
            var u = MakeEnemy(enemyId, pos);
            Enemies.Add(u);
            all.Add(u);
            Emit(FieldEventKind.Info, null, null, 0, false, $"[디버그] {u.Name} 배치");
            return true;
        }

        /// <summary>pos 에서 maxDist 안의 가장 가까운 적을 없앤다.</summary>
        public bool DebugRemoveAt(Vector2 pos, float maxDist)
        {
            FieldUnit best = null;
            float bestDist = maxDist;
            foreach (var e in Enemies)
            {
                float d = Vector2.Distance(e.Pos, pos);
                if (d <= bestDist) { bestDist = d; best = e; }
            }
            if (best == null) return false;
            RemoveEnemy(best);
            Emit(FieldEventKind.Info, null, null, 0, false, $"[디버그] {best.Name} 제거");
            return true;
        }

        /// <summary>pos 근처 적의 전술을 다음 것으로 (돌진 → 후방 침입 → 도주 · 견제 → 후방 우선 공격).</summary>
        public bool DebugCycleStrategyAt(Vector2 pos, float maxDist)
        {
            FieldUnit best = null;
            float bestDist = maxDist;
            foreach (var e in Enemies)
            {
                float d = Vector2.Distance(e.Pos, pos);
                if (d <= bestDist) { bestDist = d; best = e; }
            }
            if (best == null) return false;
            var order = FieldData.PlayerStrategies;
            int i = System.Array.IndexOf(order, best.Strategy);
            best.Strategy = order[(i + 1) % order.Length];
            best.FlankDone = false;
            best.FlankEdgeY = 0f;
            Emit(FieldEventKind.Info, null, null, 0, false, $"[디버그] {best.Name} 전술 → {FieldData.StrategyName(best.Strategy)}");
            return true;
        }

        public void DebugClearEnemies()
        {
            while (Enemies.Count > 0) RemoveEnemy(Enemies[Enemies.Count - 1]);
            Emit(FieldEventKind.Info, null, null, 0, false, "[디버그] 적 전부 제거");
        }

        /// <summary>스테이지 첫 웨이브 배치로 되돌린다 (아군 위치 · 상태는 그대로).</summary>
        public void DebugResetEnemies()
        {
            if (IsOver) return;
            SpawnWave(0);
        }

        public void DebugResetCooldowns()
        {
            foreach (var u in Players)
                for (int i = 0; i < u.Cooldowns.Length; i++) u.Cooldowns[i] = 0f;
            Emit(FieldEventKind.Info, null, null, 0, false, "[디버그] 아군 쿨타임 초기화");
        }

        public void DebugResetMeters()
        {
            foreach (var u in Players) { u.DamageDealt = 0; u.DamageTaken = 0; }
            foreach (var u in Enemies) { u.DamageDealt = 0; u.DamageTaken = 0; }
            MeterStart = Time;
            Emit(FieldEventKind.Info, null, null, 0, false, "[디버그] 피해 집계 초기화");
        }

        void RemoveEnemy(FieldUnit u)
        {
            Enemies.Remove(u);
            all.Remove(u);
            foreach (var x in all) if (x.TauntTarget == u) { x.TauntTarget = null; x.TauntTimer = 0f; }
        }

        void CheckEnd()
        {
            for (int i = 0; i < Enemies.Count; i++)
            {
                var e = Enemies[i];
                if (e.Alive || e.RewardGiven) continue;
                e.RewardGiven = true;
                EarnedExp += e.EnemyBase.Exp;
                EarnedGold += e.EnemyBase.Gold;
            }

            if (AllDead(Players))
            {
                Phase = FieldPhase.Lost;
                Emit(FieldEventKind.Info, null, null, 0, false, "파티가 전멸했다...");
                return;
            }
            if (Stage.IsTest || !AllDead(Enemies)) return;

            if (WaveIndex + 1 >= WaveCount)
            {
                Phase = FieldPhase.Won;
                Emit(FieldEventKind.Info, null, null, 0, false, $"{Stage.Name} 클리어!");
            }
            else
            {
                Phase = FieldPhase.WaveClear;
                waveTimer = FieldConst.WaveInterval;
            }
        }

        // ───────────── 유닛 행동 ─────────────

        void TickTimers(FieldUnit u, float dt)
        {
            u.StunTimer = Mathf.Max(0f, u.StunTimer - dt);
            u.RootTimer = Mathf.Max(0f, u.RootTimer - dt);
            for (int i = 0; i < u.FollowUps.Count; i++) u.FollowUps[i].Delay -= dt;
            u.ChannelTimer = Mathf.Max(0f, u.ChannelTimer - dt);
            u.CastTimer = Mathf.Max(0f, u.CastTimer - dt);
            u.CreepTimer = Mathf.Max(0f, u.CreepTimer - dt);
            u.ArmorTimer = Mathf.Max(0f, u.ArmorTimer - dt);
            u.ToughUpTimer = Mathf.Max(0f, u.ToughUpTimer - dt);
            u.ToughDownTimer = Mathf.Max(0f, u.ToughDownTimer - dt);
            u.AirTimer = Mathf.Max(0f, u.AirTimer - dt);
            u.HeldTimer = Mathf.Max(0f, u.HeldTimer - dt);
            if (u.OffMapTimer > 0f)
            {
                u.OffMapTimer -= dt;
                if (u.OffMapTimer <= 0f)
                {
                    u.OffMapTimer = 0f;
                    if (u.ArrivalHit != null && !u.Dashing)   // 낙하 착지
                    {
                        u.Pos = u.LandPos;
                        ClampToField(u);
                        var strike = u.ArrivalHit; u.ArrivalHit = null; strike();
                    }
                }
            }
            if (u.Barrage != null) BarrageStep(u, dt);

            // 화염의 팔 자해: 매초 최대 체력 5%, 체력 1 까지만 (확정 수치라 '수치 미정 피해 0' 과 무관하게 적용)
            if (u.FireArm)
            {
                u.FireArmBurn += dt;
                if (u.FireArmBurn >= 1f - 0.0001f)
                {
                    u.FireArmBurn -= 1f;
                    u.C.Hp = Mathf.Max(1, u.C.Hp - Mathf.CeilToInt(u.C.Stats.HP * FireArmSelfRatio));
                }
            }
            u.KiteDirTimer = Mathf.Max(0f, u.KiteDirTimer - dt);
            u.BreakTimer = Mathf.Max(0f, u.BreakTimer - dt);
            u.BreakCooldown = Mathf.Max(0f, u.BreakCooldown - dt);
            u.StaggerTimer = Mathf.Max(0f, u.StaggerTimer - dt);
            u.StaggerImmuneTimer = Mathf.Max(0f, u.StaggerImmuneTimer - dt);
            u.CounterTimer = Mathf.Max(0f, u.CounterTimer - dt);
            u.HuntTimer = Mathf.Max(0f, u.HuntTimer - dt);
            if (u.ReloadTimer > 0f)
            {
                u.ReloadTimer -= dt;
                if (u.ReloadTimer <= 0f) { u.ReloadTimer = 0f; u.Ammo = GunnerAmmo; }
            }
            if (u.PushTimer > 0f) PushStep(u, dt);
            u.ActionTimer = Mathf.Max(0f, u.ActionTimer - dt);
            for (int i = 0; i < u.Cooldowns.Length; i++) u.Cooldowns[i] = Mathf.Max(0f, u.Cooldowns[i] - dt);

            u.GuardTimer = Mathf.Max(0f, u.GuardTimer - dt);
            u.C.Guarding = u.GuardTimer > 0f;

            u.BuffTimer = Mathf.Max(0f, u.BuffTimer - dt);
            if (u.BuffTimer <= 0f) u.BuffMul = 1f;

            u.TauntTimer = Mathf.Max(0f, u.TauntTimer - dt);
            if (u.TauntTimer <= 0f) u.TauntTarget = null;

            if (u.Passive != null && u.Passive.RegenPct > 0f && u.C.Hp < u.C.Stats.HP)
            {
                u.RegenCarry += u.C.Stats.HP * u.Passive.RegenPct * dt;
                if (u.RegenCarry >= 1f)
                {
                    int add = Mathf.FloorToInt(u.RegenCarry);
                    u.RegenCarry -= add;
                    u.C.Hp = Mathf.Min(u.C.Stats.HP, u.C.Hp + add);
                }
            }
        }

        void Think(FieldUnit u, float dt)
        {
            if (u.StunTimer > 0f) { CancelCast(u); return; }       // 기절은 슈퍼아머를 무시하고 시전을 끊는다
            if (u.Dashing) { DashMove(u, dt); return; }
            if (u.Hidden || u.HeldTimer > 0f || (u.AirTimer > 0f && u.AirHeight > 0f && !u.IsPlayer)) return;

            var target = PickTarget(u);
            for (int i = 0; i < u.FollowUps.Count; i++)
            {
                var f = u.FollowUps[i];
                if (f.Delay > 0.0001f) continue;
                if (NeedsWindup(f.Skill) && u.Casting != null) continue;    // 다른 시전이 끝난 뒤
                u.FollowUps.RemoveAt(i--);
                u.PrimaryTarget = f.Primary;
                if (!NeedsWindup(f.Skill) || target == null || !BeginSkill(u, f.Skill, target)) UseGeneric(u, f.Skill, target);
            }
            if (u.Casting != null) { CastStep(u, dt); return; }
            if (u.CastTimer > 0f)
            {
                // 사용 중: 천천히 이동하는 스킬만 대상에게 다가간다
                if (u.CreepTimer > 0f && u.CastTarget != null && u.CastTarget.Alive && u.PushTimer <= 0f)
                    MoveToward(u, u.CastTarget.Pos, FieldConst.DashStopDist * 0.9f, dt * CreepSpeedMul);
                return;
            }
            if (target == null || u.CounterTimer > 0f || u.ChannelTimer > 0f) return;   // 카운터 자세: 아무것도 하지 않음

            if (u.ActionTimer <= 0f && TryAct(u, target))
                u.ActionTimer = Interval(u);

            // 밀려나는 중 · 경직 중: 걷지 않는다 (걸어서 넉백을 상쇄하던 문제). 공격 · 스킬은 가능 (사용자 2026-11-26).
            if ((u.PushTimer > 0f || u.StaggerTimer > 0f) && u.BreakTimer <= 0f) return;   // 돌파 중에는 멈추지 않는다
            if (!u.Alive || u.RootTimer > 0f || u.Dashing || u.MoonShadow) return;   // 월영: 이동하지 않음
            Move(u, target, dt);
        }

        // ───────────── 컨셉 규칙 (재단 · 월영 · 점화 · 충돌) ─────────────

        void UpdateAltar(float dt)
        {
            if (!HasAltar || EyeOpen) return;
            Altar += dt * (1f + EclipseCount);     // 매초 +1, 이클립스 1명당 +1
            if (Altar >= AltarEye) OpenEye();
        }

        void OpenEye()
        {
            Altar = AltarEye;
            EyeOpen = true;
            Emit(FieldEventKind.Info, null, null, 0, false, "악신의 눈이 내려다본다.");
            foreach (var u in Players) if (u.Alive && u.MoonShadowPassive) BeginMoonShadow(u);
        }

        void BeginMoonShadow(FieldUnit u)
        {
            u.NormalSkills = u.Skills;
            var shadow = LookupSkills(ConceptCharacters.MoonShadowSkills);
            // 쿨타임 칸 수(장착 수)에 맞춘다
            var fit = new FieldSkillDef[u.Cooldowns.Length];
            for (int i = 0; i < fit.Length; i++) fit[i] = shadow[i % shadow.Length];
            u.Skills = fit;
            for (int i = 0; i < u.Cooldowns.Length; i++) u.Cooldowns[i] = 0f;
            u.MoonShadow = true;
            u.ShadowTrace = 0;
            Emit(FieldEventKind.Info, u, null, 0, false, $"{u.Name} — 월영");
        }

        void EndMoonShadow(FieldUnit u)
        {
            if (u.NormalSkills != null) u.Skills = u.NormalSkills;
            u.MoonShadow = false;
            u.NormalSkills = null;
        }

        void AddIgnite(FieldUnit u, float amount)
        {
            if (u.Def == null || !u.Def.FireArmTrait || u.FireArm) return;
            u.Ignite = Mathf.Min(IgniteMax, u.Ignite + amount);
            if (u.Ignite >= IgniteMax)
            {
                u.FireArm = true;
                Emit(FieldEventKind.Info, u, null, 0, false, $"{u.Name} — 화염의 팔");
            }
        }

        /// <summary>밀려나는 중. 충돌 기절 스킬이면 다른 유닛 · 벽에 닿을 때 작은 파동 + 기절.</summary>
        void PushStep(FieldUnit u, float dt)
        {
            float t = Mathf.Min(dt, u.PushTimer);
            Vector2 before = u.Pos;
            // 처음에 확 밀리고 끝에서 감속 (남은 시간 비율, 구간 중간값이라 총 이동은 설정 거리와 같다)
            Vector2 move = u.PushVel * ((u.PushTimer - t * 0.5f) / FieldConst.PushTime) * t;
            u.Pos += move;
            u.PushTimer -= t;
            ClampToField(u);
            if (!u.PushCollide) return;

            bool hitWall = (u.Pos - before).sqrMagnitude < move.sqrMagnitude * 0.25f;
            FieldUnit other = null;
            foreach (var o in all)
            {
                if (o == u || !o.Alive) continue;
                if (Vector2.Distance(o.Pos, u.Pos) < FieldConst.Radius * 1.9f) { other = o; break; }
            }
            if (!hitWall && other == null) return;

            u.PushTimer = 0f;
            u.PushCollide = false;
            u.StunTimer = Mathf.Max(u.StunTimer, CollideStunTime);
            if (other != null && other.IsPlayer == u.IsPlayer) other.StunTimer = Mathf.Max(other.StunTimer, CollideStunTime);
            Vector2 at = other != null ? (u.Pos + other.Pos) * 0.5f : u.Pos;
            var e = AddEffect(FieldEffectKind.Pulse, u, at, 0.9f, SkillColor.Spirit);
            if (e != null) e.Theme = FxTheme.Spirit;
        }

        public void DebugOpenEye() { if (HasAltar && !EyeOpen) OpenEye(); }

        public void DebugIgnite() { foreach (var u in Players) AddIgnite(u, IgniteMax); }

        void DashMove(FieldUnit u, float dt)
        {
            Vector2 d = u.DashTo - u.Pos;
            float step = u.DashSpeed * dt;
            if (d.magnitude <= step)
            {
                u.Pos = u.DashTo;
                ClampToField(u);
                if (u.ArrivalHit != null) { var strike = u.ArrivalHit; u.ArrivalHit = null; strike(); }
                if (u.HasReturn) { u.HasReturn = false; u.DashTo = u.ReturnTo; }
                else u.Dashing = false;
                return;
            }
            u.Pos += d.normalized * step;
            ClampToField(u);
        }

        // 전략방식별 이동 (기획 v0.2 7장)
        void Move(FieldUnit u, FieldUnit target, float dt)
        {
            float dist = Vector2.Distance(u.Pos, target.Pos);
            bool inRange = dist <= u.Range;

            // 공용 규칙 (사용자 2026-11-26): 모든 유닛은 타게팅 대상을 향해 사거리 안에 들어갈 때까지 이동한다.
            switch (u.Strategy)
            {
                case Strategy.Charge:
                case Strategy.Backline:
                case Strategy.Hold:
                    if (!inRange) MoveToward(u, target.Pos, u.Range * FieldConst.ApproachRatio, dt);
                    break;

                case Strategy.Flank:
                    // 후방 침입: 적 앞라인과 마주치지 않도록 가장자리로 크게 돌아 대상 뒤로 들어간다.
                    if (!u.FlankDone)
                    {
                        float side = u.IsPlayer ? 1f : -1f;              // 아군은 +x 쪽으로 진격
                        // 대상 뒤 지점. 대상이 벽 가까이 있으면 필드 안으로 제한한다 (밖이면 영원히 도착 못 해 벽에 붙어 있던 문제)
                        float behindX = Mathf.Clamp(target.Pos.x + side * FlankBehind,
                                                    FieldConst.Radius + FlankWallGap, FieldConst.Width - FieldConst.Radius - FlankWallGap);
                        if ((u.Pos.x - behindX) * side >= -FlankArrive               // 대상 뒤에 (거의) 도착
                            || Mathf.Abs(u.Pos.x - target.Pos.x) <= FlankArrive)     // 대상과 같은 세로줄까지 왔다
                            u.FlankDone = true;
                        else
                        {
                            float edgeY = FlankEdge(u);
                            if (Mathf.Abs(u.Pos.y - edgeY) > 0.3f && (behindX - u.Pos.x) * side > 3f)
                                MoveToward(u, new Vector2(u.Pos.x + side * 0.6f, edgeY), 0f, dt);   // 먼저 가장자리로
                            else
                                MoveToward(u, new Vector2(behindX, edgeY), 0f, dt);                  // 가장자리를 따라 뒤로
                            break;
                        }
                    }
                    if (!inRange) MoveToward(u, target.Pos, u.Range * FieldConst.ApproachRatio, dt);
                    break;

                case Strategy.Kite:
                    KiteMove(u, target, inRange, dt);
                    break;

                case Strategy.Escort:
                {
                    var ward = EscortWard(u);
                    if (ward != null && Vector2.Distance(u.Pos, ward.Pos) > FieldConst.EscortDist)
                        MoveToward(u, ward.Pos, FieldConst.EscortDist * 0.8f, dt);
                    else if (!inRange) MoveToward(u, target.Pos, u.Range * FieldConst.ApproachRatio, dt);
                    break;
                }
            }
        }

        // ───────────── 도주 · 견제 (사용자 2026-11-26) ─────────────

        void KiteMove(FieldUnit u, FieldUnit target, bool inRange, float dt)
        {
            var foes = u.IsPlayer ? Enemies : Players;
            int close = 0;
            float nearest = float.MaxValue;
            foreach (var f in foes)
            {
                if (!f.Targetable) continue;
                float d = Vector2.Distance(u.Pos, f.Pos);
                nearest = Mathf.Min(nearest, d);
                if (d < FieldConst.FleeDist) close++;
            }

            // 돌파: 적 2명 이상에게 가장자리로 몰렸거나 빠져나갈 길이 전부 막혔을 때
            if (u.BreakTimer <= 0f && u.BreakCooldown <= 0f && close >= BreakFoes)
            {
                bool atEdge = u.Pos.x < KiteWallMargin || u.Pos.x > FieldConst.Width - KiteWallMargin
                           || u.Pos.y < KiteWallMargin || u.Pos.y > FieldConst.Height - KiteWallMargin;
                bool blocked;
                Vector2 way = EscapeDir(u, true, out blocked);
                bool openBlocked;
                EscapeDir(u, false, out openBlocked);
                if (atEdge || openBlocked) Breakout(u, way);
            }

            if (u.BreakTimer > 0f)
            {
                // 가속해서 완전히 빠져나간다. 벽에 닿으면 방향을 다시 고른다.
                Vector2 before = u.Pos;
                u.Pos += u.KiteDir * (u.Mov * BreakSpeedMul * dt);
                ClampToField(u);
                if ((u.Pos - before).magnitude < u.Mov * BreakSpeedMul * dt * 0.5f) u.KiteDir = EscapeDir(u, true, out _);
                return;
            }

            if (nearest < FieldConst.FleeDist)
            {
                bool hitWall = !InField(u.Pos + u.KiteDir * 0.5f);
                if (u.KiteDirTimer <= 0f || hitWall)
                {
                    u.KiteDir = EscapeDir(u, false, out _);
                    u.KiteDirTimer = KiteHold;
                }
                u.Pos += u.KiteDir * (u.Mov * dt);
                ClampToField(u);
            }
            else if (!inRange) MoveToward(u, target.Pos, u.Range * FieldConst.ApproachRatio, dt);
        }

        static bool InField(Vector2 p) => p.x >= FieldConst.Radius && p.x <= FieldConst.Width - FieldConst.Radius
                                       && p.y >= FieldConst.Radius && p.y <= FieldConst.Height - FieldConst.Radius;

        /// <summary>
        /// 도주 방향: 후보마다 KiteLook 앞 지점을 평가한다. 적 전부에게서 멀고, 벽에 덜 막히고, 진로에 적이 없는 쪽.
        /// ignoreBlockers = 돌파용 (길 막는 적은 밀어낼 것이므로 감점을 줄인다). blocked = 고른 방향에 길 막는 적이 있는가.
        /// </summary>
        Vector2 EscapeDir(FieldUnit u, bool ignoreBlockers, out bool blocked)
        {
            var foes = u.IsPlayer ? Enemies : Players;
            Vector2 best = u.IsPlayer ? Vector2.left : Vector2.right;
            float bestScore = float.MinValue;
            blocked = false;
            for (int i = 0; i < KiteDirections; i++)
            {
                float a = i / (float)KiteDirections * Mathf.PI * 2f;
                Vector2 dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                Vector2 end = u.Pos + dir * KiteLook;
                end.x = Mathf.Clamp(end.x, FieldConst.Radius, FieldConst.Width - FieldConst.Radius);
                end.y = Mathf.Clamp(end.y, FieldConst.Radius, FieldConst.Height - FieldConst.Radius);

                float travel = Vector2.Distance(u.Pos, end);          // 벽에 막히면 짧다
                float score = travel * 2f;
                float closest = KiteSense;
                bool pathBlocked = false;
                foreach (var f in foes)
                {
                    if (!f.Targetable || Vector2.Distance(u.Pos, f.Pos) > KiteSense) continue;
                    float d = Vector2.Distance(end, f.Pos);
                    closest = Mathf.Min(closest, d);
                    score += Mathf.Min(d, KiteSense) * 0.35f;
                    Vector2 ab = end - u.Pos;
                    float tt = ab.sqrMagnitude > 0.0001f ? Mathf.Clamp01(Vector2.Dot(f.Pos - u.Pos, ab) / ab.sqrMagnitude) : 0f;
                    if (tt > 0.05f && Vector2.Distance(u.Pos + ab * tt, f.Pos) < KiteBlockWidth) pathBlocked = true;
                }
                score += closest * 1.5f;
                // 도착 지점이 벽 · 구석에서 멀수록 좋다 (다음에 또 몰리지 않게)
                score += Mathf.Min(end.x, FieldConst.Width - end.x, 3f) * 0.6f + Mathf.Min(end.y, FieldConst.Height - end.y, 3f) * 0.6f;
                if (pathBlocked) score -= ignoreBlockers ? 1f : 6f;
                if (score > bestScore) { bestScore = score; best = dir; blocked = pathBlocked; }
            }
            return best;
        }

        /// <summary>돌파: 주변과 진로의 적을 옆으로 밀어내고 BreakDuration 동안 가속.</summary>
        void Breakout(FieldUnit u, Vector2 way)
        {
            u.KiteDir = way;
            u.BreakTimer = BreakDuration;
            u.BreakCooldown = BreakCooldownTime;
            u.PushTimer = 0f;
            u.StaggerTimer = 0f;
            u.StaggerImmuneTimer = BreakDuration;
            foreach (var f in u.IsPlayer ? Enemies : Players)
            {
                if (!f.Targetable) continue;
                Vector2 to = f.Pos - u.Pos;
                float along = Vector2.Dot(to, way);
                float side = Mathf.Abs(to.x * way.y - to.y * way.x);
                bool near = to.magnitude <= BreakRadius;
                bool onPath = along > 0f && along <= KiteLook + 1f && side < KiteBlockWidth;
                if (!near && !onPath) continue;
                // 진로 앞의 적은 길 옆으로, 뒤 · 옆의 적은 바깥쪽으로
                Vector2 perp = new Vector2(-way.y, way.x) * (Vector2.Dot(to, new Vector2(-way.y, way.x)) >= 0f ? 1f : -1f);
                Vector2 push = along > 0f ? perp * 1f - way * 0.25f : to.normalized - way * 0.5f;
                Knockback(f, push, BreakKnockLevel);
            }
            AddEffect(FieldEffectKind.Pulse, u, u.Pos, BreakRadius, Color.white);
            Emit(FieldEventKind.Skill, u, null, 0, false, "돌파!");
        }

        /// <summary>후방 침입이 따라갈 가장자리 y: 적들의 평균 y 에서 먼 쪽 (처음 한 번 정한다).</summary>
        float FlankEdge(FieldUnit u)
        {
            if (u.FlankEdgeY > 0f) return u.FlankEdgeY;
            // 위 · 아래 가장자리 중 길목(가장자리 근처 띠)에 상대가 덜 있는 쪽. 같으면 상대 평균 위치의 반대쪽.
            float topY = FlankEdgeMargin, botY = FieldConst.Height - FlankEdgeMargin;
            float topCrowd = 0f, botCrowd = 0f, sum = 0f; int n = 0;
            foreach (var f in u.IsPlayer ? Enemies : Players)
            {
                if (!f.Targetable) continue;
                sum += f.Pos.y; n++;
                topCrowd += Mathf.Max(0f, FlankLane - Mathf.Abs(f.Pos.y - topY));
                botCrowd += Mathf.Max(0f, FlankLane - Mathf.Abs(f.Pos.y - botY));
            }
            bool top;
            if (Mathf.Abs(topCrowd - botCrowd) > 0.5f) top = topCrowd < botCrowd;
            else if (n > 0) top = sum / n > FieldConst.Height * 0.5f;
            else top = u.Pos.y < FieldConst.Height * 0.5f;
            u.FlankEdgeY = top ? topY : botY;
            return u.FlankEdgeY;
        }

        static void MoveToward(FieldUnit u, Vector2 point, float stopDist, float dt)
        {
            Vector2 d = point - u.Pos;
            float dist = d.magnitude;
            if (dist <= stopDist || dist < 0.0001f) return;
            float step = Mathf.Min(u.Mov * dt, dist - stopDist);
            u.Pos += d / dist * step;
            ClampToField(u);
        }

        static void ClampToField(FieldUnit u)
        {
            u.Pos.x = Mathf.Clamp(u.Pos.x, FieldConst.Radius, FieldConst.Width - FieldConst.Radius);
            u.Pos.y = Mathf.Clamp(u.Pos.y, FieldConst.Radius, FieldConst.Height - FieldConst.Radius);
        }

        // 겹친 유닛을 서로 밀어낸다 (한 번)
        void Separate()
        {
            float minDist = FieldConst.Radius * 2f;
            for (int i = 0; i < all.Count; i++)
            {
                if (!all[i].Targetable) continue;
                for (int j = i + 1; j < all.Count; j++)
                {
                    if (!all[j].Targetable) continue;
                    Vector2 d = all[j].Pos - all[i].Pos;
                    float dist = d.magnitude;
                    if (dist >= minDist) continue;
                    Vector2 dir = dist < 0.0001f ? new Vector2(0f, 1f) : d / dist;
                    float push = (minDist - dist) * 0.5f;
                    all[i].Pos -= dir * push;
                    all[j].Pos += dir * push;
                    ClampToField(all[i]);
                    ClampToField(all[j]);
                }
            }
        }

        // ───────────── 타겟 ─────────────

        // 타겟 점수 = 어그로 배율 / 거리 (기획 v0.2 7장). 도발은 점수를 무시한다.
        FieldUnit PickTarget(FieldUnit u)
        {
            if (u.TauntTimer > 0f && u.TauntTarget != null && u.TauntTarget.Alive) return u.TauntTarget;
            if (u.HuntTimer > 0f)
                foreach (var f in u.IsPlayer ? Enemies : Players) if (f.Alive && f.Marked) return f;   // 몰이 사냥
            if (u.Strategy == Strategy.Backline) return BacklineTarget(u);
            if (u.Strategy == Strategy.Flank) return FlankTarget(u);

            Vector2 anchor = u.Pos;
            if (u.Strategy == Strategy.Escort)
            {
                var ward = EscortWard(u);
                if (ward != null) anchor = ward.Pos;
            }

            var foes = u.IsPlayer ? Enemies : Players;
            FieldUnit best = null;
            float bestScore = -1f;
            for (int i = 0; i < foes.Count; i++)
            {
                var f = foes[i];
                if (!f.Targetable) continue;
                float dist = Mathf.Max(0.5f, Vector2.Distance(anchor, f.Pos));
                float score = f.AggroMul / dist;
                if (u.Strategy == Strategy.Flank && f.Range >= FieldConst.RangedThreshold)
                    score *= FieldConst.RangedPriority;
                if (score > bestScore) { bestScore = score; best = f; }
            }
            return best;
        }

        /// <summary>
        /// 후방 침입의 대상: 상대의 후열(원거리 · 서포터)을 먼저, 그중 가장 뒤쪽. 앞라인(가깝거나 어그로가 높은 적)에는 끌리지 않는다.
        /// (예전에는 일반 타겟 점수를 써서 돌진 중인 앞라인을 노리는 문제가 있었다.)
        /// </summary>
        FieldUnit FlankTarget(FieldUnit u)
        {
            float side = u.IsPlayer ? 1f : -1f;
            FieldUnit best = null;
            float bestScore = float.MinValue;
            foreach (var f in u.IsPlayer ? Enemies : Players)
            {
                if (!f.Targetable) continue;
                bool backline = f.Range >= FieldConst.RangedThreshold || f.Role == Role.Supporter || f.Role == Role.Healer;
                float score = (backline ? FlankBacklineBonus : 0f) + f.Pos.x * side * 10f - Vector2.Distance(u.Pos, f.Pos) * 0.1f;
                if (score > bestScore) { bestScore = score; best = f; }
            }
            return best;
        }

        /// <summary>후방 우선 공격: 아군 진영에서 가장 먼(가장 뒤에 있는) 적.</summary>
        FieldUnit BacklineTarget(FieldUnit u)
        {
            float side = u.IsPlayer ? 1f : -1f;
            FieldUnit best = null;
            float bestScore = float.MinValue;
            foreach (var f in u.IsPlayer ? Enemies : Players)
            {
                if (!f.Targetable) continue;
                float score = f.Pos.x * side * 10f - Vector2.Distance(u.Pos, f.Pos) * 0.1f;
                if (score > bestScore) { bestScore = score; best = f; }
            }
            return best;
        }

        FieldUnit EscortWard(FieldUnit u)
        {
            var allies = u.IsPlayer ? Players : Enemies;
            FieldUnit best = null;
            for (int i = 0; i < allies.Count; i++)
            {
                var a = allies[i];
                if (a == u || !a.Alive) continue;
                if (best == null || a.HpRatio < best.HpRatio) best = a;
            }
            return best;
        }

        FieldUnit NearestFoe(FieldUnit u) => NearestFoeWithin(u, float.MaxValue);

        FieldUnit NearestFoeWithin(FieldUnit u, float range)
        {
            var foes = u.IsPlayer ? Enemies : Players;
            FieldUnit best = null;
            float bestDist = range;
            for (int i = 0; i < foes.Count; i++)
            {
                if (!foes[i].Targetable) continue;
                float d = Vector2.Distance(u.Pos, foes[i].Pos);
                if (d <= bestDist) { bestDist = d; best = foes[i]; }
            }
            return best;
        }

        // ───────────── 스킬 ─────────────

        bool TryAct(FieldUnit u, FieldUnit target)
        {
            for (int i = 0; i < u.Skills.Length; i++)
            {
                if (u.Cooldowns[i] > 0f) continue;
                var s = u.Skills[i];
                if (s.Pistol)
                {
                    // 특수탄: 장전만 해 두고 다음 기본 공격들이 특수탄으로 나간다 (기본 공격 속도, 사용자 2026-11-26)
                    if (u.ReloadTimer > 0f || u.SpecialAmmo > 0 || Vector2.Distance(u.Pos, target.Pos) > s.Range) continue;
                    u.SpecialSkill = s;
                    u.SpecialAmmo = Mathf.Max(1, s.Shots);
                    u.Cooldowns[i] = s.EffectiveCooldown;
                    AfterSkill(u, i, s);
                    Emit(FieldEventKind.Skill, u, null, 0, false, $"{s.Name} 장전");
                    break;
                }
                var use = u.FireArm && !string.IsNullOrEmpty(s.FireArmVariant) ? FieldData.Skill(s.FireArmVariant) ?? s : s;
                if (use.Id == "agni_fire_fist" && u.HpRatio <= LowHpRatio && u.Passive != null && u.Passive.Id == "agni_burn")
                    use = FieldData.Skill("agni_okyeom") ?? use;
                if (!BeginSkill(u, use, target)) continue;
                u.Cooldowns[i] = s.EffectiveCooldown;
                AfterSkill(u, i, s);
                return true;
            }

            if (u.SpecialAmmo > 0 && !u.MoonShadow && u.ReloadTimer <= 0f)
            {
                var sp = u.SpecialSkill;
                if (Vector2.Distance(u.Pos, target.Pos) > sp.Range) return false;
                Vector2 dir = target.Pos - u.Pos;
                if (dir.sqrMagnitude > 0.0001f) u.Facing = dir.normalized;
                Projectiles.Add(new FieldProjectile { Pos = u.Pos, Dir = u.Facing, Left = sp.Range + 1f, Source = u, Skill = sp });
                u.SpecialAmmo--;
                AddIgnite(u, IgniteBasic);
                return true;
            }

            // 기본 공격 (월영: 하지 않음 · 장전 중: 불가)
            if (u.MoonShadow || u.ReloadTimer > 0f) return false;
            if (Vector2.Distance(u.Pos, target.Pos) > u.Range) return false;
            Vector2 d = target.Pos - u.Pos;
            if (d.sqrMagnitude > 0.0001f) u.Facing = d.normalized;

            if (FieldData.IsRangedBasic(u.Basic))
            {
                var shot = FieldData.BasicShot(u.Basic, u.BasicColor, u.Range);
                float dist = Vector2.Distance(u.Pos, target.Pos);
                Projectiles.Add(new FieldProjectile { Pos = u.Pos, Dir = u.Facing, Left = u.Range + 1f, Source = u, Skill = shot, Total = dist, BasicKnock = u.BasicKnock });
            }
            else
            {
                Hit(u, target, 1f, 0f, "공격", false);
                Knockback(target, target.Pos - u.Pos, u.BasicKnock);
                if (u.Basic != BasicKind.None)
                {
                    var e = AddEffect(FieldEffectKind.Swing, u, u.Pos + u.Facing * 0.7f, 1f, u.BasicColor);
                    if (e != null) { e.Count = (int)u.Basic; e.End = target.Pos; }
                }
            }
            if (u.Def != null && u.Def.Gunner && --u.Ammo <= 0) { u.Ammo = 0; u.ReloadTimer = ReloadTime; }
            AddIgnite(u, IgniteBasic);
            return true;
        }

        // ───────────── 시전 동작 · 슈퍼아머 ─────────────

        const float CreepSpeedMul = 0.5f;       // 사용 중 천천히 이동: 평소 이동속도 × 이 값
        const float MinUseTime = 0.35f;         // '사용 중' 최소 시간
        const float LowHpRatio = 0.3f;
        const float FireArmSelfRatio = 0.05f;   // 화염의 팔: 자신은 매초 최대 체력 5% (2.5% 의 2배)
        const float ToughDuration = 5f;

        static bool NeedsWindup(FieldSkillDef s) => s.CastTime > 0f || s.Lunge > 0f || s.Approach > 0f;
        static bool HasAction(FieldSkillDef s) => s.Armor || s.Creep > 0f || NeedsWindup(s);

        /// <summary>스킬 시작. 준비 동작이 없으면 바로 발동, 있으면 시전 상태로 들어간다. 쓸 수 없으면 false.</summary>
        bool BeginSkill(FieldUnit u, FieldSkillDef s, FieldUnit target)
        {
            if (!NeedsWindup(s))
            {
                if (!UseSkill(u, s, target)) return false;
                u.CastTarget = u.PrimaryTarget != null && u.PrimaryTarget.Alive ? u.PrimaryTarget : target;
                AfterExecute(u, s);
                return true;
            }

            float own = (s.Shape == SkillShape.Around || s.Shape == SkillShape.Cone || s.Effect == SkillEffect.Spin) && s.Radius > 0f
                        ? s.Radius : s.Range > 0f ? s.Range : u.Range;
            float reach = s.Approach > 0f ? s.Approach : s.CastTime > 0f ? own : own + s.Lunge;
            var t = PickFoe(u, s, target, reach);
            if (t == null) return false;

            Vector2 to = t.Pos - u.Pos;
            if (to.sqrMagnitude > 0.0001f) u.Facing = to.normalized;
            float wait = s.CastTime;
            if (s.Lunge > 0f) wait = Mathf.Max(wait, ConceptCombat.LungeTime);
            if (s.Approach > 0f) wait = Mathf.Max(wait, Mathf.Max(0f, to.magnitude - FieldConst.DashStopDist) / FieldConst.DashSpeed);
            u.Casting = s;
            u.CastTarget = t;
            u.CastWait = wait;
            u.LungeLeft = s.Lunge;
            if (s.Armor) u.ArmorTimer = Mathf.Max(u.ArmorTimer, wait + FieldConst.Dt);
            if (s.Aim) Fx(FieldEffectKind.Aim, u, u.Pos, t.Pos, 1f, s.FxColor, 0f, wait);
            if (s.Aim && recordEvents && Effects.Count > 0) Effects[Effects.Count - 1].Target = t;
            if (s.Approach > 0f) Fx(FieldEffectKind.Trail, u, u.Pos, t.Pos, 0.5f, s.FxColor, 0f, wait);
            if (s.CastFx != FieldEffectKind.None)
            {
                var ce = Fx(s.CastFx, u, u.Pos, t.Pos, 1f, s.FxColor, 0f, s.CastFx == FieldEffectKind.BladeSlam ? wait + 0.15f : wait);
                if (ce != null) ce.Target = u;            // 시전자를 따라다닌다
            }
            return true;
        }

        void CastStep(FieldUnit u, float dt)
        {
            var s = u.Casting;
            var t = u.CastTarget;
            if (t != null && t.Alive && u.PushTimer <= 0f)
            {
                float gap = Vector2.Distance(u.Pos, t.Pos) - FieldConst.DashStopDist;
                if (s.Approach > 0f && gap > 0f)
                    MoveBy(u, (t.Pos - u.Pos).normalized * Mathf.Min(gap, FieldConst.DashSpeed * dt));
                else if (u.LungeLeft > 0f && gap > 0f)
                {
                    float step = Mathf.Min(u.LungeLeft, gap, ConceptCombat.LungeDistance / ConceptCombat.LungeTime * dt);
                    u.LungeLeft -= step;
                    MoveBy(u, (t.Pos - u.Pos).normalized * step);
                }
            }

            u.CastWait -= dt;
            if (u.CastWait > 0.0001f) return;
            u.Casting = null;
            u.LungeLeft = 0f;
            if (t == null || !t.Alive) t = PickTarget(u);
            if (t == null) return;
            if (!UseSkill(u, s, t))
            {
                if (s.Shape != SkillShape.None) Emit(FieldEventKind.Skill, u, null, 0, false, $"{s.Name} (빗나감)");
                return;
            }
            u.CastTarget = t;
            AfterExecute(u, s);
        }

        static void MoveBy(FieldUnit u, Vector2 delta) { u.Pos += delta; ClampToField(u); }

        /// <summary>발동 직후: '사용 중' 시간 · 슈퍼아머 · 천천히 이동 · 자해 · 강인함.</summary>
        void AfterExecute(FieldUnit u, FieldSkillDef s)
        {
            if (s.SelfDamagePct > 0f)
                u.C.Hp = Mathf.Max(1, u.C.Hp - Mathf.RoundToInt(u.C.Stats.HP * s.SelfDamagePct));
            if (s.ToughUp > 0f)
            {
                u.ToughUp = u.HpRatio <= LowHpRatio && s.ToughUpLowHp > 0f ? s.ToughUpLowHp : s.ToughUp;
                u.ToughUpTimer = ToughDuration;
            }
            if (s.BarrageTime > 0f)
            {
                u.Barrage = s;
                u.BarrageLeft = Mathf.Max(1, s.Hits);
                u.BarrageInterval = s.BarrageTime / u.BarrageLeft;
                u.BarrageNext = 0f;
            }
            if (!HasAction(s) && s.BarrageTime <= 0f && !s.HideWhileUsing) return;

            float active = Mathf.Max(MinUseTime, s.BarrageTime);
            if (s.Creep > 0f) { active = Mathf.Max(active, s.Creep); u.CreepTimer = s.Creep; }
            active = Mathf.Max(active, s.Root);
            if (u.Dashing)
            {
                float dash = Vector2.Distance(u.Pos, u.DashTo);
                if (u.HasReturn) dash += Vector2.Distance(u.DashTo, u.ReturnTo);
                active = Mathf.Max(active, dash / FieldConst.DashSpeed);
            }
            if (s.Channel) active = Mathf.Max(active, s.FollowDelay);
            u.CastTimer = Mathf.Max(u.CastTimer, active);
            if (s.Armor) u.ArmorTimer = Mathf.Max(u.ArmorTimer, active);
            if (s.HideWhileUsing) u.OffMapTimer = Mathf.Max(u.OffMapTimer, active);   // 사용 중 맵 이탈
        }

        /// <summary>나눠 때리는 연타 (염호멸연격 · 심연난무).</summary>
        void BarrageStep(FieldUnit u, float dt)
        {
            var s = u.Barrage;
            if (!u.Alive || u.StunTimer > 0f) { u.Barrage = null; return; }
            u.BarrageNext -= dt;
            if (u.BarrageNext > 0f) return;
            u.BarrageNext += u.BarrageInterval;
            if (s.BarrageCone)
            {
                foreach (var f in FoesAround(u, u.Pos, s.Radius))
                {
                    Vector2 to = f.Pos - u.Pos;
                    if (to.sqrMagnitude < 0.0001f || Vector2.Angle(u.Facing, to) <= s.Angle * 0.5f) SkillHit(u, f, s);
                }
            }
            else
            {
                var t = u.CastTarget;
                if (t != null && t.Targetable)
                {
                    SkillHit(u, t, s);
                    Vector2 d = t.Pos - u.Pos;
                    if (d.sqrMagnitude > 0.0001f) u.Facing = d.normalized;
                }
            }
            if (--u.BarrageLeft <= 0) u.Barrage = null;
        }

        void LaunchUp(FieldUnit f, float time)
        {
            if (f.SuperArmor) return;                  // 띄우기도 밀림이라 슈퍼아머는 무시 [해석]
            f.AirTimer = f.AirTotal = time * (1f - f.Toughness / 100f);
            f.AirHeight = 1.2f;
        }

        static void CancelCast(FieldUnit u)
        {
            if (u.Casting == null && u.CastTimer <= 0f && u.ArmorTimer <= 0f) return;
            u.Casting = null;
            u.CastTimer = 0f;
            u.CreepTimer = 0f;
            u.ArmorTimer = 0f;
            u.Barrage = null;
        }

        /// <summary>디버그: 아군 체력을 25% 로 (저체력 조건 확인용).</summary>
        public void DebugHurtAllies()
        {
            foreach (var u in Players) if (u.Alive) u.C.Hp = Mathf.Max(1, u.C.Stats.HP / 4);
            Emit(FieldEventKind.Info, null, null, 0, false, "[디버그] 아군 체력 25%");
        }

        /// <summary>스킬을 쓴 뒤: 점화 · 그림자 흔적 · 번갈아 바뀌는 스킬.</summary>
        void AfterSkill(FieldUnit u, int slot, FieldSkillDef s)
        {
            AddIgnite(u, IgniteSkill);
            if (u.MoonShadow)
            {
                u.ShadowTrace = Mathf.Min(ShadowTraceMax, u.ShadowTrace + 1);
                if (s.Id == "yw_shadow_cut") u.ShadowTrace = 0;
                if (u.ShadowTrace >= ShadowTraceMax)
                    for (int k = 0; k < u.Skills.Length; k++)
                        if (u.Skills[k].Id == "yw_shadow_open") { u.Skills[k] = FieldData.Skill("yw_shadow_cut"); u.Cooldowns[k] = 0f; }
            }
            if (!string.IsNullOrEmpty(s.Next))
            {
                var next = FieldData.Skill(s.Next);
                if (next != null) u.Skills[slot] = next;
            }
        }

        bool UseSkill(FieldUnit u, FieldSkillDef s, FieldUnit target)
        {
            float range = s.Range > 0f ? s.Range : u.Range;
            var allies = u.IsPlayer ? Players : Enemies;
            var foes = u.IsPlayer ? Enemies : Players;
            Vector2 toTarget = target.Pos - u.Pos;
            if (toTarget.sqrMagnitude > 0.0001f) u.Facing = toTarget.normalized;
            if (s.Shape != SkillShape.None) return UseGeneric(u, s, target);

            switch (s.Effect)
            {
                // ── 스티움 고유 ──
                case SkillEffect.Spin:     // 회전베기: 전방위
                case SkillEffect.Pulse:    // 어둠의 파동: 퍼지는 원
                {
                    var hit = FoesWithin(u, s.Radius);
                    if (hit.Count == 0) return false;
                    int sum = 0;
                    foreach (var f in hit) { sum += SkillHit(u, f, s); Knockback(f, f.Pos - u.Pos, s.KnockLevel); }
                    AddEffect(s.Effect == SkillEffect.Spin ? FieldEffectKind.Spin : FieldEffectKind.Pulse, u, u.Pos, s.Radius, s.FxColor);
                    Emit(FieldEventKind.Skill, u, null, sum, false, AreaText(s, hit.Count, sum));
                    return true;
                }

                case SkillEffect.Smash:    // 강격검: 전방 가까운 적 + 원형 파괴
                {
                    var t = Vector2.Distance(u.Pos, target.Pos) <= range ? target : NearestFoeWithin(u, range);
                    if (t == null) return false;
                    int dmg = SkillHit(u, t, s);
                    Knockback(t, t.Pos - u.Pos, s.KnockLevel);
                    AddEffect(FieldEffectKind.Smash, u, t.Pos, s.Radius, s.FxColor);
                    Emit(FieldEventKind.Skill, u, t, dmg, false, $"{s.Name} → {t.Name} {dmg}");
                    return true;
                }

                case SkillEffect.DarkWave: // 어둠의 검기: 일직선 투사체
                {
                    var t = Vector2.Distance(u.Pos, target.Pos) <= range ? target : NearestFoeWithin(u, range);
                    if (t == null) return false;
                    Vector2 d = t.Pos - u.Pos;
                    d = d.sqrMagnitude > 0.0001f ? d.normalized : u.Facing;
                    u.Facing = d;
                    Projectiles.Add(new FieldProjectile { Pos = u.Pos, Dir = d, Left = range, Source = u, Skill = s });
                    // (Shot 기본값 Arc = 아치형 검기)
                    Emit(FieldEventKind.Skill, u, null, 0, false, s.Name);
                    return true;
                }

                case SkillEffect.Grasp:    // 어둠의 손아귀: 큰 원이 한곳으로 (끌어당기기 · 수치 미정)
                {
                    if (FoesWithin(u, s.Radius).Count == 0) return false;
                    AddEffect(FieldEffectKind.Grasp, u, u.Pos, s.Radius, s.FxColor);
                    Emit(FieldEventKind.Skill, u, null, 0, false, s.Name);
                    return true;
                }

                case SkillEffect.Harvest:  // 어둠 거두기: 어둠 잠식이 있는 적만. 잠식을 전부 소모 (사용자 2026-11-26)
                {
                    var hit = new List<FieldUnit>();
                    foreach (var f in FoesWithin(u, s.Radius)) if (f.DarkErosion > 0) hit.Add(f);
                    if (hit.Count == 0) return false;
                    int sum = 0;
                    var e = AddEffect(FieldEffectKind.Harvest, u, u.Pos, s.Radius, s.FxColor);
                    if (e != null) e.Points = new Vector2[hit.Count];
                    for (int i = 0; i < hit.Count; i++)
                    {
                        var f = hit[i];
                        if (e != null) e.Points[i] = f.Pos;
                        int dmg = Damage.Fixed(f.C, ErosionDamagePerStack * f.DarkErosion, u.DamageMul);
                        f.DarkErosion = 0;
                        ApplyHit(u, f, new HitResult { Amount = dmg }, s.Name, true);
                        sum += dmg;
                    }
                    int heal = Mathf.RoundToInt(sum * HarvestHealRatio);
                    u.C.Hp = Mathf.Min(u.C.Stats.HP, u.C.Hp + heal);
                    Emit(FieldEventKind.Skill, u, null, sum, false, $"{s.Name} — {hit.Count}명, 피해 {sum}, 회복 {heal}");
                    return true;
                }

                case SkillEffect.Damage:
                {
                    var t = Vector2.Distance(u.Pos, target.Pos) <= range ? target : NearestFoeWithin(u, range);
                    if (t == null) return false;
                    for (int h = 0; h < s.Hits && t.Alive; h++)
                        Hit(u, t, s.Coef, s.BonusCrit, s.Name, s.Hits > 1);
                    return true;
                }

                case SkillEffect.Guard:
                    if (u.GuardTimer > 0f || NearestFoeWithin(u, range) == null) return false;
                    u.GuardTimer = s.Duration;
                    u.C.Guarding = true;
                    Emit(FieldEventKind.Skill, u, u, 0, false, s.Name);
                    return true;

                case SkillEffect.Taunt:
                {
                    bool any = false;
                    for (int i = 0; i < foes.Count; i++)
                    {
                        var f = foes[i];
                        if (!f.Targetable || Vector2.Distance(u.Pos, f.Pos) > range) continue;
                        f.TauntTarget = u;
                        f.TauntTimer = s.Duration;
                        any = true;
                    }
                    if (any) Emit(FieldEventKind.Skill, u, u, 0, false, s.Name);
                    return any;
                }

                case SkillEffect.Heal:
                {
                    FieldUnit t = null;
                    for (int i = 0; i < allies.Count; i++)
                    {
                        var a = allies[i];
                        if (!a.Alive || a.HpRatio >= FieldConst.HealBelow) continue;
                        if (Vector2.Distance(u.Pos, a.Pos) > range) continue;
                        if (t == null || a.HpRatio < t.HpRatio) t = a;
                    }
                    if (t == null) return false;
                    int amount = Damage.Heal(u.C, s.Coef, u.BuffTimer > 0f ? u.BuffMul : 1f);
                    int before = t.C.Hp;
                    t.C.Hp = Mathf.Min(t.C.Stats.HP, t.C.Hp + amount);
                    Emit(FieldEventKind.Heal, u, t, t.C.Hp - before, false, s.Name);
                    return true;
                }

                case SkillEffect.AtkBuff:
                {
                    if (NearestFoe(u) == null || u.BuffTimer > 0f) return false;
                    for (int i = 0; i < allies.Count; i++)
                    {
                        if (!allies[i].Alive) continue;
                        allies[i].BuffTimer = s.Duration;
                        allies[i].BuffMul = 1f + s.Value;
                    }
                    Emit(FieldEventKind.Skill, u, u, 0, false, s.Name);
                    return true;
                }
            }
            return false;
        }

        void Hit(FieldUnit src, FieldUnit tgt, float coef, float bonusCrit, string label, bool multi)
        {
            var r = Damage.Compute(src.C, tgt.C, coef * src.DamageMul, bonusCrit, rng, src.BuffTimer > 0f ? src.BuffMul : 1f);
            ApplyHit(src, tgt, r, label, false);
        }

        /// <summary>v0.2 스킬 피해 (물리 + 마법). 준 피해를 돌려준다.</summary>
        int SkillHit(FieldUnit src, FieldUnit tgt, FieldSkillDef s, bool quiet = true)
        {
            float mul = (src.BuffTimer > 0f ? src.BuffMul : 1f) * src.DamageMul;
            var r = Damage.ComputeSkill(src.C, tgt.C, s.Coef, s.MagicBase, s.MagicCoef, rng, mul, tgt.DarkErosion);
            ApplyHit(src, tgt, r, s.Name, quiet);
            return r.Amount;
        }

        void ApplyHit(FieldUnit src, FieldUnit tgt, HitResult r, string label, bool quiet)
        {
            tgt.LastHitTime = Time;
            if (src.AppliesErosion && src.IsPlayer != tgt.IsPlayer) tgt.DarkErosion = Mathf.Min(MaxErosion, tgt.DarkErosion + 1);
            if (tgt.CounterTimer > 0f && tgt.CounterSkill != null && src.IsPlayer != tgt.IsPlayer)
            {
                tgt.CounterTimer = 0f;
                tgt.RootTimer = 0f;
                tgt.FollowUps.Add(new FollowUpEntry { Skill = tgt.CounterSkill, Delay = 0f });   // 즉시 반격
            }
            if (!tgt.Immortal) tgt.C.Hp = Mathf.Max(0, tgt.C.Hp - r.Amount);
            tgt.DamageTaken += r.Amount;
            src.DamageDealt += r.Amount;
            if (recordEvents)
                Events.Add(new FieldEvent { Time = Time, Kind = FieldEventKind.Hit, Source = src, Target = tgt,
                                            Amount = r.Amount, Crit = r.Crit, Text = label, Quiet = quiet });
            if (!tgt.Alive) Emit(FieldEventKind.Death, src, tgt, 0, false, $"{tgt.Name}이(가) 쓰러졌다.");
        }

        void UpdateProjectiles(float dt)
        {
            for (int i = 0; i < Projectiles.Count; i++)
            {
                var p = Projectiles[i];
                if (p.Delay > 0f)
                {
                    p.Delay -= dt;
                    if (p.Source.Alive) p.Pos = p.Source.Pos; // 대기 중에는 시전자 위치에서 출발
                    continue;
                }
                float step = FieldConst.ProjectileSpeed * dt;
                p.Pos += p.Dir * step;
                p.Left -= step;
                p.Traveled += step;
                if (p.Total <= 0f) p.Total = p.Left + p.Traveled;

                var sk = p.Skill;
                var foes = p.Source.IsPlayer ? Enemies : Players;
                for (int k = 0; k < foes.Count; k++)
                {
                    var f = foes[k];
                    if (!f.Targetable || p.Hit.Contains(f)) continue;
                    if (Vector2.Distance(p.Pos, f.Pos) > FieldConst.ProjectileHitRadius + FieldConst.Radius) continue;
                    p.Hit.Add(f);
                    if (sk.Shape == SkillShape.None)
                    {
                        SkillHit(p.Source, f, sk, quiet: false);
                        Knockback(f, p.Dir, sk.KnockLevel);
                        AddEffect(FieldEffectKind.Burst, p.Source, p.Pos, 0.6f, sk.FxColor);
                    }
                    else
                    {
                        Apply(p.Source, f, sk, 1f, f.Pos - p.Dir);   // 넉백 방향 = 날아온 방향
                        if (p.BasicKnock > 0) Knockback(f, p.Dir, p.BasicKnock);
                        if (sk.ExplodeRadius > 0f)
                            foreach (var o in FoesAround(p.Source, p.Pos, sk.ExplodeRadius)) if (o != f) Apply(p.Source, o, sk, 1f, p.Pos);
                        Fx(sk.Fx2, p.Source, p.Pos, p.Pos, sk.Radius > 0f ? sk.Radius : 0.6f, sk.FxColor);
                    }
                    if (!(sk.Shape != SkillShape.None && sk.Pierce)) { p.Done = true; break; }
                }
                if (p.Left <= 0f || p.Pos.x < 0f || p.Pos.x > FieldConst.Width || p.Pos.y < 0f || p.Pos.y > FieldConst.Height)
                    p.Done = true;
            }
            Projectiles.RemoveAll(p => p.Done);
        }

        List<FieldUnit> FoesWithin(FieldUnit u, float radius)
        {
            var foes = u.IsPlayer ? Enemies : Players;
            var list = new List<FieldUnit>();
            for (int i = 0; i < foes.Count; i++)
                if (foes[i].Alive && Vector2.Distance(u.Pos, foes[i].Pos) <= radius + FieldConst.Radius) list.Add(foes[i]);
            return list;
        }

        FieldEffect AddEffect(FieldEffectKind kind, FieldUnit src, Vector2 origin, float radius, Color color)
        {
            if (!recordEvents || kind == FieldEffectKind.None) return null;
            var e = new FieldEffect { Kind = kind, Start = Time, Origin = origin, End = origin, Dir = src.Facing, Radius = radius, Source = src, Color = color, Theme = src.Theme };
            Effects.Add(e);
            return e;
        }

        FieldEffect Fx(FieldEffectKind kind, FieldUnit src, Vector2 origin, Vector2 end, float radius, Color color,
                       float delay = 0f, float duration = 0f, int count = 0, Vector2[] points = null)
        {
            var e = AddEffect(kind, src, origin, radius, color);
            if (e == null) return null;
            e.End = end;
            Vector2 d = end - origin;
            if (d.sqrMagnitude > 0.0001f) e.Dir = d.normalized;
            e.Delay = delay;
            e.Duration = duration;
            e.Count = count;
            e.Points = points;
            return e;
        }

        // ───────────── 컨셉 스킬 공통 실행 (ConceptSkills.cs) ─────────────

        /// <summary>모양 · 대상 규칙 · 효과를 데이터대로 실행한다. 쓸 수 없으면 false (쿨타임 안 돎).</summary>
        bool UseGeneric(FieldUnit u, FieldSkillDef s, FieldUnit current)
        {
            var allies = u.IsPlayer ? Players : Enemies;
            float range = s.Range > 0f ? s.Range : u.Range;
            float roll = s.Roll ? (float)rng.NextDouble() : 1f;   // "A ~ B" 결과 (1 = 최대)
            Color col = s.FxColor;
            FieldUnit t = null;

            switch (s.Shape)
            {
                case SkillShape.Around:
                {
                    var hit = FoesAround(u, u.Pos, s.Radius);
                    if (hit.Count == 0) return false;
                    foreach (var f in hit) Apply(u, f, s, roll, u.Pos);
                    var around = Fx(s.Fx, u, u.Pos, u.Pos, s.Radius, col, 0f, s.FxTime, s.Hits);
                    if (around != null && s.Fx == FieldEffectKind.Whirl) around.Target = u;
                    Fx(s.Fx2, u, u.Pos, u.Pos, s.Radius, col, s.FxDelay2);
                    break;
                }

                case SkillShape.Target:
                case SkillShape.TargetArea:
                {
                    t = PickFoe(u, s, current, range);
                    if (t == null) return false;
                    u.PrimaryTarget = t;
                    float radius = s.Shape == SkillShape.TargetArea ? Mathf.Lerp(s.Radius, FieldConst.Width, s.Roll ? roll : 0f) : s.Radius;
                    var hit = s.Shape == SkillShape.TargetArea ? FoesAround(u, t.Pos, radius) : new List<FieldUnit> { t };
                    Vector2 at = t.Pos;
                    if (s.CrossLaser)   // 역십자가: 십자 방향으로 맵 전체를 꿰뚫는다
                    {
                        foreach (var f in FoesOnSegment(u, new Vector2(0f, at.y), new Vector2(FieldConst.Width, at.y), FieldConst.PierceWidth)) if (!hit.Contains(f)) hit.Add(f);
                        foreach (var f in FoesOnSegment(u, new Vector2(at.x, 0f), new Vector2(at.x, FieldConst.Height), FieldConst.PierceWidth)) if (!hit.Contains(f)) hit.Add(f);
                    }
                    if (s.PullToSelf) Fx(s.Fx, u, u.Pos, at, s.Radius, col);          // 사슬 · 손아귀: 선이 먼저
                    foreach (var f in hit) Apply(u, f, s, roll, u.Pos);
                    if (!s.PullToSelf)
                    {
                        bool lob = s.Fx == FieldEffectKind.Lob || s.Fx == FieldEffectKind.Beam;
                        var main = Fx(s.Fx, u, lob ? u.Pos : at, at, radius, col, 0f, s.FxTime, s.Hits);
                        if (main != null && s.Creep > 0f) main.Target = t;      // 연타하며 다가가는 스킬
                        if (main != null && (s.Fx == FieldEffectKind.CrossFall || s.Fx == FieldEffectKind.Maw || s.Fx == FieldEffectKind.Punches)) main.Target = t;
                        if (main != null && s.BarrageTime > 0f) { main.Duration = s.BarrageTime; main.Count = Mathf.Max(1, s.Hits); }
                        if (main != null && s.Fx == FieldEffectKind.Cross)
                        {
                            if (s.CrossLaser) main.Count = 1;       // 화면: 레이저 십자
                            else main.Target = t;                   // 침묵 선언: 십자가가 대상을 따라다님
                        }
                    }
                    if (s.Fx == FieldEffectKind.FireLine && recordEvents)
                    {
                        // 시전자에서 대상 방향으로 맵 끝까지 뻗는 선
                        var line = Effects[Effects.Count - 1];
                        Vector2 dir = (at - u.Pos).sqrMagnitude > 0.0001f ? (at - u.Pos).normalized : u.Facing;
                        line.Origin = u.Pos;
                        line.Dir = dir;
                        line.End = u.Pos + dir * (FieldConst.Width + FieldConst.Height);
                    }
                    if (s.ImpactFrame > 0f) ImpactTimer = s.ImpactFrame;
                    float d2 = s.Fx == FieldEffectKind.Lob ? FieldConst.LobTime : s.FxDelay2;
                    Vector2 at2 = s.Fx2AtFront ? u.Pos + u.Facing * 1.2f : at;
                    var f2 = Fx(s.Fx2, u, at2, at2, radius, col, d2, s.Fx2 == FieldEffectKind.Freeze ? 1.5f : 0f);
                    if (f2 != null && s.Fx2 == FieldEffectKind.Freeze)
                    {
                        var frozen = new List<FieldUnit>();
                        foreach (var o in u.IsPlayer ? Enemies : Players) if (o.Targetable) frozen.Add(o);
                        f2.Units = frozen.ToArray();
                    }
                    if (s.Fx == FieldEffectKind.Harvest)
                    {
                        var e = Effects.Count > 0 && recordEvents ? Effects[Effects.Count - 1] : null;
                        if (e != null && e.Kind == FieldEffectKind.Harvest) { e.Points = new[] { at }; e.Origin = u.Pos; }
                    }
                    break;
                }

                case SkillShape.Projectile:
                {
                    t = PickFoe(u, s, current, range);
                    if (t == null) return false;
                    u.PrimaryTarget = t;
                    Vector2 d = (t.Pos - u.Pos).sqrMagnitude > 0.0001f ? (t.Pos - u.Pos).normalized : u.Facing;
                    u.Facing = d;
                    for (int i = 0; i < Mathf.Max(1, s.Shots); i++)
                        Projectiles.Add(new FieldProjectile { Pos = u.Pos, Dir = d, Left = range + 1f, Source = u, Skill = s, Delay = i * FieldConst.ShotInterval });
                    break;
                }

                case SkillShape.Pierce:
                {
                    t = PickFoe(u, s, current, range);
                    if (t == null) return false;
                    u.PrimaryTarget = t;
                    Vector2 d = (t.Pos - u.Pos).sqrMagnitude > 0.0001f ? (t.Pos - u.Pos).normalized : u.Facing;
                    u.Facing = d;
                    Vector2 end = u.Pos + d * range;
                    float width = s.Radius > 0f ? s.Radius : FieldConst.PierceWidth;
                    var hit = FoesOnSegment(u, u.Pos, end, width);
                    if (s.Roll && hit.Count > 0)   // "처음 적중하는 적 ~ 모든 적"
                        hit = hit.GetRange(0, Mathf.Clamp(Mathf.CeilToInt(hit.Count * roll), 1, hit.Count));
                    foreach (var f in hit) Apply(u, f, s, roll, u.Pos);
                    Fx(s.Fx, u, u.Pos, end, width, col);
                    if (s.Fx2 == FieldEffectKind.BladeSlam) Fx(s.Fx2, u, u.Pos, end, width, col, 0f, 0.35f);
                    break;
                }

                case SkillShape.Cone:
                {
                    Vector2 face = current != null && (current.Pos - u.Pos).sqrMagnitude > 0.0001f ? (current.Pos - u.Pos).normalized : u.Facing;
                    var hit = new List<FieldUnit>();
                    foreach (var f in FoesAround(u, u.Pos, s.Radius))
                    {
                        Vector2 to = f.Pos - u.Pos;
                        if (to.sqrMagnitude < 0.0001f || Vector2.Angle(face, to) <= s.Angle * 0.5f) hit.Add(f);
                    }
                    if (hit.Count == 0) return false;
                    u.Facing = face;
                    foreach (var f in hit) Apply(u, f, s, roll, u.Pos);
                    var e = Fx(s.Fx, u, u.Pos, u.Pos + face * s.Radius, s.Radius, col, 0f, s.BarrageTime);
                    if (e != null) e.Count = s.BarrageTime > 0f ? Mathf.Max(1, s.Hits) : Mathf.RoundToInt(s.Angle);
                    if (s.BarrageTime > 0f && e != null) e.Target = u;
                    if (s.Combo)
                        foreach (var f in hit)
                        {
                            var m = Fx(FieldEffectKind.Multi, u, f.Pos, f.Pos, 0.8f, col, 0.2f, 0f, 4);
                            if (m != null) m.Target = f;
                            var x = Fx(FieldEffectKind.Burst, u, f.Pos, f.Pos, 1.0f, col, 0.65f);
                            if (x != null) x.Target = f;
                        }
                    Fx(s.Fx2, u, u.Pos, u.Pos, s.Radius * 0.6f, col, s.FxDelay2);
                    break;
                }

                case SkillShape.Dash:
                case SkillShape.Leap:
                {
                    t = PickFoe(u, s, current, range);
                    if (t == null) return false;
                    u.PrimaryTarget = t;
                    Vector2 start = u.Pos;
                    Vector2 d = (t.Pos - start).sqrMagnitude > 0.0001f ? (t.Pos - start).normalized : u.Facing;
                    u.Facing = d;
                    Vector2 end = s.DashBehind ? t.Pos + d * (FieldConst.DashStopDist * 1.2f) : t.Pos - d * FieldConst.DashStopDist;
                    end.x = Mathf.Clamp(end.x, FieldConst.Radius, FieldConst.Width - FieldConst.Radius);
                    end.y = Mathf.Clamp(end.y, FieldConst.Radius, FieldConst.Height - FieldConst.Radius);

                    var hit = s.Pierce ? FoesOnSegment(u, start, end, FieldConst.PierceWidth) : new List<FieldUnit>();
                    if (!hit.Contains(t)) hit.Add(t);
                    if (s.Radius > 0f && s.Shape == SkillShape.Leap)
                        foreach (var f in FoesAround(u, end, s.Radius)) if (!hit.Contains(f)) hit.Add(f);
                    if (s.Radius > 0f && s.Shape == SkillShape.Dash && s.Stun > 0f)
                        foreach (var f in FoesAround(u, end, s.Radius)) if (!hit.Contains(f)) hit.Add(f);
                    // 한 지점을 치는 돌진 · 도약은 도착하는 순간 타격 (먼저 밀리면 때린 장면에서 안 밀려 보였다).
                    // 경로를 꿰뚫는 돌진은 지나가며 맞으므로 바로 적용.
                    if (s.Pierce) foreach (var f in hit) Apply(u, f, s, roll, start);
                    else
                    {
                        var targets = hit;
                        var primary = t;
                        u.ArrivalHit = () =>
                        {
                            u.PrimaryTarget = primary;
                            foreach (var f in targets) if (f.Alive) Apply(u, f, s, roll, start);
                        };
                    }

                    u.DashSpeed = FieldConst.DashSpeed;
                    float travel = Vector2.Distance(start, end) / FieldConst.DashSpeed;
                    if (s.OffMapTime > 0f)
                    {
                        // 수직으로 맵을 벗어났다가 대상 위치에 낙하
                        u.OffMapTimer = s.OffMapTime;
                        u.LandPos = end;
                        Fx(FieldEffectKind.LeapOff, u, start, start, 1f, col, 0f, 0.3f);
                        Fx(FieldEffectKind.Landing, u, end, end, Mathf.Max(1f, s.Radius), col, 0f, s.OffMapTime);
                        Fx(s.Fx2, u, end, end, s.Radius > 0f ? s.Radius : 0.9f, col, s.OffMapTime);
                        if (s.Armor) u.ArmorTimer = Mathf.Max(u.ArmorTimer, s.OffMapTime);
                        break;
                    }
                    if (s.HangTime > 0f)
                    {
                        travel = s.HangTime;
                        u.DashSpeed = Mathf.Max(1f, Vector2.Distance(start, end) / s.HangTime);
                        u.AirTimer = u.AirTotal = s.HangTime;
                        u.AirHeight = 2.2f;
                    }
                    u.Dashing = true;
                    u.DashTo = end;
                    if (s.DashReturn) { u.HasReturn = true; u.ReturnTo = start; }
                    Fx(s.Fx, u, start, end, 0.5f, col, 0f, travel);
                    if (s.ImpactCone) Fx(FieldEffectKind.FanWave, u, t.Pos, t.Pos + d * 3f, 3f, col, travel);
                    var e2 = Fx(s.Fx2, u, t.Pos, t.Pos, s.Radius > 0f ? s.Radius : 0.9f, col, travel + s.FxDelay2, s.FxTime, s.Hits);
                    if (e2 != null && s.Fx2 == FieldEffectKind.Blink) e2.Target = t;
                    break;
                }

                case SkillShape.AllEnemies:
                case SkillShape.RandomEnemies:
                {
                    var foes = new List<FieldUnit>();
                    foreach (var f in u.IsPlayer ? Enemies : Players) if (f.Targetable) foes.Add(f);
                    if (foes.Count == 0) return false;
                    var picks = foes;
                    if (s.Shape == SkillShape.RandomEnemies)
                    {
                        picks = new List<FieldUnit>();
                        for (int i = 0; i < Mathf.Max(1, s.Shots); i++) picks.Add(foes[rng.Next(foes.Count)]);
                    }
                    var pts = new Vector2[picks.Count];
                    for (int i = 0; i < picks.Count; i++) { pts[i] = picks[i].Pos; Apply(u, picks[i], s, roll, u.Pos); }
                    var ae = Fx(s.Fx, u, u.Pos, u.Pos, s.Radius, col, 0f, s.FxTime, picks.Count, pts);
                    if (ae != null && s.Fx == FieldEffectKind.Links) ae.Units = picks.ToArray();   // 흑색 사슬: 적들에게 붙어 따라다님
                    Fx(s.Fx2, u, u.Pos, u.Pos, s.Radius, col, s.FxDelay2);
                    break;
                }

                case SkillShape.AllyLowest:
                {
                    FieldUnit best = null;
                    foreach (var a in allies)
                    {
                        if (!a.Alive || (a == u && allies.Count > 1)) continue;
                        if (s.NeedsAllyStatus && a.StunTimer <= 0f) continue;
                        if (best == null || a.HpRatio < best.HpRatio) best = a;
                    }
                    if (best == null) return false;
                    bool lob = s.Fx == FieldEffectKind.Lob;
                    Fx(s.Fx, lob ? u : best, u.Pos, best.Pos, 0.6f, col, 0f, s.FxTime);
                    if (s.NeedsAllyStatus) best.StunTimer = 0f;   // 정화: 상태이상 해제
                    Fx(s.Fx2, best, best.Pos, best.Pos, 0.6f, col, lob ? FieldConst.LobTime : 0f, s.FxTime);
                    if (s.AlsoSelf && best != u)
                    {
                        Fx(lob ? s.Fx2 : s.Fx, u, u.Pos, u.Pos, 0.6f, col, 0f, s.FxTime);
                    }
                    break;
                }

                case SkillShape.AllyAll:
                    if (NearestFoe(u) == null) return false;
                    if (s.Fx2 == FieldEffectKind.Coin) Fx(s.Fx2, u, u.Pos, u.Pos, 0.6f, col, 0f, 0.9f);
                    foreach (var a in allies)
                    {
                        if (!a.Alive) continue;
                        if (s.WolvesOnly && (a.Def == null || !a.Def.Wolf)) continue;   // 몰이 사냥: 아군 늑대만
                        if (s.Hunt > 0f) a.HuntTimer = s.Hunt;
                        Fx(s.Fx, a, a.Pos, a.Pos, 0.6f, col, 0f, s.FxTime);
                        if (s.Fx2 != FieldEffectKind.Coin) Fx(s.Fx2, a, a.Pos, a.Pos, 0.6f, col, s.FxDelay2, s.FxTime);
                    }
                    break;

                case SkillShape.Self:
                    if (NearestFoe(u) == null) return false;
                    if (!string.IsNullOrEmpty(s.CounterSkill))
                    {
                        u.CounterTimer = s.FxTime;
                        u.CounterSkill = FieldData.Skill(s.CounterSkill);
                    }
                    Fx(s.Fx, u, u.Pos, u.Pos, 0.6f, col, 0f, s.FxTime);
                    Fx(s.Fx2, u, u.Pos, u.Pos, 0.6f, col, s.FxDelay2, s.FxTime);
                    break;

                case SkillShape.GrabSlam:   // 에피타이저: 잡고, 주변 랜덤 위치에 내려찍고, 뱉어낸다
                {
                    if (u.Grabbed != null && (u.Grabbed.HeldTimer <= 0f || !u.Grabbed.Alive)) u.Grabbed = null;   // 끊긴 이전 잡기 정리
                    bool first = u.Grabbed == null;
                    if (first)
                    {
                        t = PickFoe(u, s, current, range);
                        if (t == null) return false;
                        u.Grabbed = t;
                        u.BarrageLeft = Mathf.Max(1, s.Hits);       // 남은 내려찍기 횟수
                        t.HeldTimer = s.FollowDelay * (s.Hits + 1) + 0.2f;
                        var g = Fx(FieldEffectKind.Grab, u, t.Pos, t.Pos, s.Radius, col, 0f, t.HeldTimer);
                        if (g != null) g.Target = t;
                    }
                    else
                    {
                        var held = u.Grabbed;
                        if (!held.Alive) { u.Grabbed = null; return true; }
                        if (u.BarrageLeft > 0)
                        {
                            // 글러트니 주변 랜덤 위치로 옮겨 내려찍는다
                            float ang = (float)(rng.NextDouble() * Mathf.PI * 2.0), dist = 1.8f + (float)rng.NextDouble() * 1.2f;
                            held.Pos = u.Pos + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * dist;
                            ClampToField(held);
                            u.Facing = (held.Pos - u.Pos).normalized;
                            SkillHit(u, held, s);
                            foreach (var f in FoesAround(u, held.Pos, s.Radius))
                            {
                                if (f == held) continue;
                                SkillHit(u, f, s);
                                f.StunTimer = Mathf.Max(f.StunTimer, s.Stun * (1f - f.Toughness / 100f));
                                Knockback(f, f.Pos - held.Pos, 2);
                            }
                            Fx(FieldEffectKind.CrossSlam, u, held.Pos, held.Pos, s.Radius, col);
                            Fx(FieldEffectKind.Burst, u, held.Pos, held.Pos, s.Radius, col, 0.05f);
                            u.BarrageLeft--;
                        }
                        else
                        {
                            // 뱉어낸다
                            held.HeldTimer = 0f;
                            u.Grabbed = null;
                            held.StunTimer = Mathf.Max(held.StunTimer, s.Stun * (1f - held.Toughness / 100f));
                            Knockback(held, held.Pos - u.Pos, 2);
                            return true;
                        }
                    }
                    u.RootTimer = Mathf.Max(u.RootTimer, s.FollowDelay);
                    u.ChannelTimer = Mathf.Max(u.ChannelTimer, s.FollowDelay);
                    u.FollowUps.Add(new FollowUpEntry { Skill = s, Delay = s.FollowDelay, Primary = u.Grabbed });
                    if (!first) return true;
                    break;
                }

                case SkillShape.Zone:
                {
                    Vector2 at;
                    if (s.ZoneAtSelf) at = u.Pos;
                    else
                    {
                        t = PickFoe(u, s, current, range);
                        if (t == null) return false;
                        u.PrimaryTarget = t;
                        at = t.Pos;
                    }
                    foreach (var f in FoesAround(u, at, s.Radius)) Apply(u, f, s, roll, at);
                    bool lob = s.Fx == FieldEffectKind.Lob;
                    Fx(s.Fx, u, u.Pos, at, s.Radius, col);
                    Fx(s.Fx2, u, at, at, s.Radius, col, lob ? FieldConst.LobTime : 0f, s.FxTime);
                    break;
                }

                case SkillShape.Wall:
                {
                    var near = NearestFoe(u);
                    if (near == null) return false;
                    Vector2 d = (near.Pos - u.Pos).sqrMagnitude > 0.0001f ? (near.Pos - u.Pos).normalized : u.Facing;
                    Vector2 center = u.Pos + d * Mathf.Min(FieldConst.WallDist, Vector2.Distance(u.Pos, near.Pos) * 0.5f);
                    Vector2 perp = new Vector2(-d.y, d.x) * s.Radius;
                    Fx(s.Fx, u, center - perp, center + perp, 0.35f, col, 0f, s.FxTime);
                    break;
                }

                default:
                    return false;
            }

            if (s.Root > 0f) u.RootTimer = Mathf.Max(u.RootTimer, s.Root);
            if (s.ResetShortestCd) ResetShortestCooldown(u, s);
            if (!string.IsNullOrEmpty(s.FollowUp))
            {
                u.FollowUps.Add(new FollowUpEntry { Skill = FieldData.Skill(s.FollowUp), Delay = s.FollowDelay, Primary = u.PrimaryTarget });
                if (s.Channel) u.ChannelTimer = Mathf.Max(u.ChannelTimer, s.FollowDelay);
            }
            if (!s.Quiet) Emit(FieldEventKind.Skill, u, t, 0, false, t != null ? $"{s.Name} → {t.Name}" : s.Name);
            return true;
        }

        /// <summary>대상 1명에게 피해(Hits 번) · 밀쳐내기 · 기절 · 표식.</summary>
        void Apply(FieldUnit src, FieldUnit f, FieldSkillDef s, float roll, Vector2 from)
        {
            if (!s.NoDamage && s.BarrageTime <= 0f && s.Shape != SkillShape.GrabSlam)
                for (int h = 0; h < Mathf.Max(1, s.Hits); h++) SkillHit(src, f, s);
            if (s.Launch > 0f) LaunchUp(f, s.Launch);

            if (s.PullToSelf)
            {
                Vector2 to = src.Pos - f.Pos;
                float dist = Mathf.Max(0f, to.magnitude - FieldConst.DashStopDist * 1.2f);
                if (dist > 0.01f) Push(f, to.normalized * dist);
            }
            else if (s.Knock != 0f)
            {
                Vector2 away = f.Pos - from;
                if (away.sqrMagnitude < 0.0001f) away = src.Facing;
                float k = s.Knock * (s.Roll ? roll : 1f);
                if (k < 0f) k = -Mathf.Min(-k, Mathf.Max(0f, away.magnitude - FieldConst.Radius * 2f)); // 끌어당겨도 겹치지 않게
                Push(f, away.normalized * k);
                f.PushCollide = s.CollideStun;
            }
            else
            {
                bool primary = f == src.PrimaryTarget;
                if (!(primary && s.KnockSkipPrimary))
                {
                    int level = !primary && s.KnockLevelOthers > 0 ? s.KnockLevelOthers : s.KnockLevel;
                    // 주대상 제외 스킬: 주대상 자리에서 바깥쪽으로
                    Vector2 dir = s.KnockSkipPrimary && src.PrimaryTarget != null ? f.Pos - src.PrimaryTarget.Pos : f.Pos - from;
                    if (level > 0)
                    {
                        Knockback(f, dir, level);
                        f.PushCollide = s.CollideStun;
                    }
                }
            }
            if (s.Stun > 0f)
            {
                float st = s.Stun * (s.Roll ? roll : 1f) * (1f - f.Toughness / 100f);
                if (st > 0.05f) f.StunTimer = Mathf.Max(f.StunTimer, st);
            }
            if (s.ToughDown > 0f) { f.ToughDown = s.ToughDown; f.ToughDownTimer = ToughDuration; }
            if (s.Marks)
            {
                foreach (var o in src.IsPlayer ? Enemies : Players) o.Marked = false;
                f.Marked = true;
            }
        }

        /// <summary>넉백 단계(1~5)만큼 dir 방향으로 밀어낸다.</summary>
        void Knockback(FieldUnit f, Vector2 dir, int level)
        {
            float dist = FieldConst.KnockOf(level);
            if (dist <= 0f || !f.Alive || f.SuperArmor) return;
            if (dir.sqrMagnitude < 0.0001f) dir = f.IsPlayer ? Vector2.left : Vector2.right;
            Push(f, dir.normalized * dist);

            float stagger = FieldConst.KnockStagger[Mathf.Clamp(level, 0, FieldConst.MaxKnockLevel)] * (1f - f.Toughness / 100f);
            if (stagger > 0f && f.StaggerImmuneTimer <= 0f)
            {
                f.StaggerTimer = FieldConst.PushTime + stagger;
                f.StaggerImmuneTimer = f.StaggerTimer + FieldConst.StaggerImmuneTime;
            }
            if (level >= FieldConst.ShakeKnockLevel) LastHeavyKnockTime = Time;
        }

        /// <summary>남은 임팩트 프레임 (필드 정지 · 색 반전). 화면용: Step 은 영향받지 않는다.</summary>
        public float ImpactTimer { get; private set; }

        /// <summary>마지막으로 큰 넉백(4단계 이상)이 난 시각 (화면 흔들림).</summary>
        public float LastHeavyKnockTime { get; private set; } = -99f;

        static void Push(FieldUnit f, Vector2 offset)
        {
            if (f.SuperArmor) return;                       // 슈퍼아머: 밀리지도 끌려가지도 않는다
            offset *= 1f - f.Toughness / 100f;              // 강인함 1당 1% 감소
            f.PushCollide = false;
            f.PushVel = offset * (2f / FieldConst.PushTime);   // 시작 속도 (선형 감속 → 총 이동 = offset)
            f.PushTimer = FieldConst.PushTime;
        }

        /// <summary>대상 규칙(Pick)대로 사거리 안 적을 고른다.</summary>
        FieldUnit PickFoe(FieldUnit u, FieldSkillDef s, FieldUnit current, float range)
        {
            var foes = u.IsPlayer ? Enemies : Players;
            if (s.Pick == TargetPick.Current)
            {
                if (current != null && current.Alive && Vector2.Distance(u.Pos, current.Pos) <= range) return current;
                return NearestFoeWithin(u, range);
            }

            FieldUnit best = null;
            float bestScore = float.MinValue;
            foreach (var f in foes)
            {
                if (!f.Targetable) continue;
                float dist = Vector2.Distance(u.Pos, f.Pos);
                if (dist > range) continue;
                float score;
                switch (s.Pick)
                {
                    case TargetPick.Farthest: score = dist; break;
                    case TargetPick.LowestHp: score = -f.HpRatio * 1000f - dist; break;
                    case TargetPick.HighestMaxHp: score = f.C.Stats.HP * 10f - dist; break;
                    case TargetPick.HighestAtk: score = (f.C.Stats.ATK + f.C.Stats.MAG) * 10f - dist; break;
                    case TargetPick.HighestMag: score = f.C.Stats.MAG * 10f - dist; break;
                    case TargetPick.HighestHp: score = f.C.Hp * 10f - dist; break;
                    case TargetPick.Marked: if (!f.Marked) continue; score = -dist; break;
                    default: score = -dist; break;
                }
                if (score > bestScore) { bestScore = score; best = f; }
            }
            return best;
        }

        List<FieldUnit> FoesAround(FieldUnit u, Vector2 center, float radius)
        {
            var foes = u.IsPlayer ? Enemies : Players;
            var list = new List<FieldUnit>();
            foreach (var f in foes)
                if (f.Alive && Vector2.Distance(center, f.Pos) <= radius + FieldConst.Radius) list.Add(f);
            return list;
        }

        /// <summary>선분 a→b 에서 폭 width 안의 적 (a 에서 가까운 순).</summary>
        List<FieldUnit> FoesOnSegment(FieldUnit u, Vector2 a, Vector2 b, float width)
        {
            var foes = u.IsPlayer ? Enemies : Players;
            var list = new List<FieldUnit>();
            Vector2 ab = b - a;
            float len2 = Mathf.Max(0.0001f, ab.sqrMagnitude);
            foreach (var f in foes)
            {
                if (!f.Targetable) continue;
                float tt = Mathf.Clamp01(Vector2.Dot(f.Pos - a, ab) / len2);
                if (Vector2.Distance(a + ab * tt, f.Pos) <= width + FieldConst.Radius) list.Add(f);
            }
            list.Sort((x, y) => Vector2.Distance(a, x.Pos).CompareTo(Vector2.Distance(a, y.Pos)));
            return list;
        }

        static void ResetShortestCooldown(FieldUnit u, FieldSkillDef self)
        {
            int best = -1;
            for (int i = 0; i < u.Skills.Length; i++)
            {
                if (u.Skills[i] == self || u.Cooldowns[i] <= 0f) continue;
                if (best < 0 || u.Cooldowns[i] < u.Cooldowns[best]) best = i;
            }
            if (best >= 0) u.Cooldowns[best] = 0f;
        }

        static string AreaText(FieldSkillDef s, int count, int sum) => $"{s.Name} — {count}명, 피해 {sum}";

        // ───────────── 보조 ─────────────

        static float Interval(FieldUnit u) => 1f / Mathf.Max(0.1f, u.C.Stats.SPD * FieldConst.AttackPerSpd);

        static bool AllDead(List<FieldUnit> list)
        {
            for (int i = 0; i < list.Count; i++) if (list[i].Alive) return false;
            return true;
        }

        static FieldSkillDef[] LookupSkills(string[] ids)
        {
            var list = new List<FieldSkillDef>();
            if (ids != null)
            {
                foreach (var id in ids)
                {
                    var s = FieldData.Skill(id);
                    if (s != null) list.Add(s);
                    else Debug.LogError($"[TextSlayers] 없는 스킬 ID: {id}");
                }
            }
            return list.ToArray();
        }

        static Role EnemyRole(Strategy s) => FieldData.StrategyRole(s) ?? Role.Dealer;

        void Emit(FieldEventKind kind, FieldUnit src, FieldUnit tgt, int amount, bool crit, string text)
        {
            if (!recordEvents) return;
            Events.Add(new FieldEvent { Time = Time, Kind = kind, Source = src, Target = tgt, Amount = amount, Crit = crit, Text = text });
        }
    }
}
