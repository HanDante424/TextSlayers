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

    public enum FieldEffectKind { Spin, Smash, Burst, Pulse, Grasp, Harvest }

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
        public FieldUnit Source;     // 끌려오는 연출의 도착점 (현재 위치를 따라간다)
        public Vector2[] Points;     // 어둠 거두기: 대상들의 위치
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
    }

    /// <summary>필드 위의 유닛 한 명 (아군 또는 적).</summary>
    public sealed class FieldUnit
    {
        public readonly Combatant C;          // 스탯 · HP (피해 계산은 Damage.cs 가 이걸 읽는다)
        public readonly bool IsPlayer;
        public readonly Strategy Strategy;
        public readonly Role Role;
        public readonly float Mov;
        public readonly float Range;
        public readonly float AggroMul;
        public readonly FieldSkillDef[] Skills;
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
            AggroMul = strategy == Strategy.Charge ? FieldConst.ChargeAggro
                     : strategy == Strategy.Flank ? FieldConst.FlankAggro
                     : 1f;
        }

        public string Name => C.Name;
        public bool Alive => C.Alive;
        public float HpRatio => C.HpRatio;
    }

    /// <summary>
    /// 스테이지 1회 실시간 필드 전투 (기획 v0.2 7~8장). MonoBehaviour 가 아니며 고정 스텝(FieldConst.Dt)으로 진행한다.
    /// 화면 없이 FieldSim 으로 돌릴 수 있다. 피해 · 회복 계산은 Damage.cs 만 쓴다.
    /// </summary>
    public sealed class FieldBattle
    {
        public readonly FieldStageDef Stage;
        public readonly List<FieldUnit> Players = new List<FieldUnit>();
        public readonly List<FieldUnit> Enemies = new List<FieldUnit>();
        public readonly List<FieldEvent> Events = new List<FieldEvent>(); // recordEvents 가 true 일 때만 쌓인다. 화면이 비운다.
        public readonly List<FieldEffect> Effects = new List<FieldEffect>(); // recordEvents 가 true 일 때만. EffectMaxAge 지나면 지운다.
        public readonly List<FieldProjectile> Projectiles = new List<FieldProjectile>();
        public const float EffectMaxAge = 2f;

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

        public FieldBattle(FieldStageDef stage, PartySlot[] party, int level, int seed, bool recordEvents = false)
        {
            Stage = stage;
            this.recordEvents = recordEvents;
            rng = new System.Random(seed);

            for (int i = 0; i < party.Length; i++)
            {
                var slot = party[i];
                var passive = FieldData.Passive(slot.Passive);
                var stats = FieldData.ApplyPassive(FieldData.StatsAt(slot.Def, level), passive);
                var c = new Combatant(slot.Def.Name, stats, true);
                var u = new FieldUnit(c, true, slot.Strategy,
                    FieldData.CurrentRole(slot.Def, slot.Strategy, slot.Skills, slot.Passive),
                    slot.Def.Mov, slot.Def.Range, LookupSkills(slot.Skills), passive, null);
                u.DamageMul = slot.Def.NumbersSet ? 1f : 0f;
                float yFrac = (i + 1f) / (party.Length + 1f);
                u.Pos = new Vector2(FieldConst.PlayerStartX + (i % 2) * 1f, FieldConst.Height * yFrac);
                u.FlankEdgeY = u.Pos.y < FieldConst.Height * 0.5f ? 1f : FieldConst.Height - 1f;
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
            accumulator += realDt * speed;
            int steps = 0;
            while (accumulator >= FieldConst.Dt && !IsOver && steps < FieldConst.MaxStepsPerUpdate)
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
            var ids = Stage.Waves[index];
            for (int i = 0; i < ids.Length; i++)
            {
                float yFrac = (i + 1f) / (ids.Length + 1f);
                Enemies.Add(MakeEnemy(ids[i], new Vector2(FieldConst.EnemyStartX + (i % 2) * 1f, FieldConst.Height * yFrac)));
            }

            all.Clear();
            all.AddRange(Players);
            all.AddRange(Enemies);
            Phase = FieldPhase.Running;
            Emit(FieldEventKind.Wave, null, null, index + 1, false, $"[{index + 1}/{WaveCount}] 적이 나타났다!");
        }

        FieldUnit MakeEnemy(string id, Vector2 pos)
        {
            var fe = FieldData.Enemy(id);
            var eb = GameData.Enemy(id);
            var c = new Combatant(eb.Name, eb.Stats, false, eb);
            var u = new FieldUnit(c, false, fe.Strategy, EnemyRole(fe.Strategy), fe.Mov, fe.Range,
                LookupSkills(fe.Skills), null, eb);
            u.Immortal = fe.Immortal;
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

        static void TickTimers(FieldUnit u, float dt)
        {
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
            var target = PickTarget(u);
            if (target == null) return;

            if (u.ActionTimer <= 0f && TryAct(u, target))
                u.ActionTimer = Interval(u);

            if (!u.Alive) return;
            Move(u, target, dt);
        }

        // 전략방식별 이동 (기획 v0.2 7장)
        void Move(FieldUnit u, FieldUnit target, float dt)
        {
            float dist = Vector2.Distance(u.Pos, target.Pos);
            bool inRange = dist <= u.Range;

            switch (u.Strategy)
            {
                case Strategy.Charge:
                    if (!inRange) MoveToward(u, target.Pos, u.Range * FieldConst.ApproachRatio, dt);
                    break;

                case Strategy.Flank:
                    if (!u.FlankDone)
                    {
                        var wp = new Vector2(target.Pos.x, u.FlankEdgeY);
                        if (Vector2.Distance(u.Pos, wp) < FieldConst.FlankWaypointDist
                            || Mathf.Abs(u.Pos.x - target.Pos.x) < FieldConst.FlankCloseX)
                            u.FlankDone = true;
                        else
                        {
                            MoveToward(u, wp, 0f, dt);
                            break;
                        }
                    }
                    if (!inRange) MoveToward(u, target.Pos, u.Range * FieldConst.ApproachRatio, dt);
                    break;

                case Strategy.Kite:
                {
                    var nearest = NearestFoe(u);
                    if (nearest != null && Vector2.Distance(u.Pos, nearest.Pos) < FieldConst.FleeDist)
                    {
                        Vector2 away = u.Pos - nearest.Pos;
                        if (away.sqrMagnitude < 0.0001f) away = new Vector2(u.IsPlayer ? -1f : 1f, 0f);
                        u.Pos += away.normalized * (u.Mov * dt);
                        ClampToField(u);
                    }
                    else if (!inRange) MoveToward(u, target.Pos, u.Range * FieldConst.ApproachRatio, dt);
                    break;
                }

                case Strategy.Escort:
                {
                    var ward = EscortWard(u);
                    if (ward != null && Vector2.Distance(u.Pos, ward.Pos) > FieldConst.EscortDist)
                        MoveToward(u, ward.Pos, FieldConst.EscortDist * 0.8f, dt);
                    break;
                }

                default: // Hold
                    break;
            }
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
                if (!all[i].Alive) continue;
                for (int j = i + 1; j < all.Count; j++)
                {
                    if (!all[j].Alive) continue;
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
                if (!f.Alive) continue;
                float dist = Mathf.Max(0.5f, Vector2.Distance(anchor, f.Pos));
                float score = f.AggroMul / dist;
                if (u.Strategy == Strategy.Flank && f.Range >= FieldConst.RangedThreshold)
                    score *= FieldConst.RangedPriority;
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
                if (!foes[i].Alive) continue;
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
                if (!UseSkill(u, s, target)) continue;
                u.Cooldowns[i] = s.EffectiveCooldown;
                return true;
            }

            // 기본 공격
            if (Vector2.Distance(u.Pos, target.Pos) <= u.Range)
            {
                Hit(u, target, 1f, 0f, "공격", false);
                return true;
            }
            return false;
        }

        bool UseSkill(FieldUnit u, FieldSkillDef s, FieldUnit target)
        {
            float range = s.Range > 0f ? s.Range : u.Range;
            var allies = u.IsPlayer ? Players : Enemies;
            var foes = u.IsPlayer ? Enemies : Players;
            Vector2 toTarget = target.Pos - u.Pos;
            if (toTarget.sqrMagnitude > 0.0001f) u.Facing = toTarget.normalized;

            switch (s.Effect)
            {
                // ── 스티움 고유 ──
                case SkillEffect.Spin:     // 회전베기: 전방위
                case SkillEffect.Pulse:    // 어둠의 파동: 퍼지는 원
                {
                    var hit = FoesWithin(u, s.Radius);
                    if (hit.Count == 0) return false;
                    int sum = 0;
                    foreach (var f in hit) sum += SkillHit(u, f, s);
                    AddEffect(s.Effect == SkillEffect.Spin ? FieldEffectKind.Spin : FieldEffectKind.Pulse, u, u.Pos, s.Radius);
                    Emit(FieldEventKind.Skill, u, null, sum, false, AreaText(s, hit.Count, sum));
                    return true;
                }

                case SkillEffect.Smash:    // 강격검: 전방 가까운 적 + 원형 파괴
                {
                    var t = Vector2.Distance(u.Pos, target.Pos) <= range ? target : NearestFoeWithin(u, range);
                    if (t == null) return false;
                    int dmg = SkillHit(u, t, s);
                    AddEffect(FieldEffectKind.Smash, u, t.Pos, s.Radius);
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
                    Emit(FieldEventKind.Skill, u, null, 0, false, s.Name);
                    return true;
                }

                case SkillEffect.Grasp:    // 어둠의 손아귀: 큰 원이 한곳으로 (끌어당기기 · 수치 미정)
                {
                    if (FoesWithin(u, s.Radius).Count == 0) return false;
                    AddEffect(FieldEffectKind.Grasp, u, u.Pos, s.Radius);
                    Emit(FieldEventKind.Skill, u, null, 0, false, s.Name);
                    return true;
                }

                case SkillEffect.Harvest:  // 어둠 거두기: 대상들에게서 파티클이 끌려옴 (어둠 잠식 · 수치 미구현)
                {
                    var hit = FoesWithin(u, s.Radius);
                    if (hit.Count == 0) return false;
                    var e = AddEffect(FieldEffectKind.Harvest, u, u.Pos, s.Radius);
                    if (e != null)
                    {
                        e.Points = new Vector2[hit.Count];
                        for (int i = 0; i < hit.Count; i++) e.Points[i] = hit[i].Pos;
                    }
                    Emit(FieldEventKind.Skill, u, null, 0, false, $"{s.Name} ({hit.Count}명)");
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
                        if (!f.Alive || Vector2.Distance(u.Pos, f.Pos) > range) continue;
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
            var r = Damage.ComputeSkill(src.C, tgt.C, s.Coef, s.MagicBase, s.MagicCoef, rng, mul);
            ApplyHit(src, tgt, r, s.Name, quiet);
            return r.Amount;
        }

        void ApplyHit(FieldUnit src, FieldUnit tgt, HitResult r, string label, bool quiet)
        {
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
                float step = FieldConst.ProjectileSpeed * dt;
                p.Pos += p.Dir * step;
                p.Left -= step;

                var foes = p.Source.IsPlayer ? Enemies : Players;
                for (int k = 0; k < foes.Count; k++)
                {
                    var f = foes[k];
                    if (!f.Alive) continue;
                    if (Vector2.Distance(p.Pos, f.Pos) > FieldConst.ProjectileHitRadius + FieldConst.Radius) continue;
                    int dmg = SkillHit(p.Source, f, p.Skill, quiet: false);
                    AddEffect(FieldEffectKind.Burst, p.Source, p.Pos, 0.6f);
                    p.Done = true;
                    break;
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

        FieldEffect AddEffect(FieldEffectKind kind, FieldUnit src, Vector2 origin, float radius)
        {
            if (!recordEvents) return null;
            var e = new FieldEffect { Kind = kind, Start = Time, Origin = origin, Dir = src.Facing, Radius = radius, Source = src };
            Effects.Add(e);
            return e;
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
