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

        public FieldPhase Phase { get; private set; }
        public int WaveIndex { get; private set; }
        public int WaveCount => Stage.Waves.Length;
        public float Time { get; private set; }
        public bool Retreated { get; private set; }
        public bool TimedOut { get; private set; }
        public int EarnedExp { get; private set; }
        public int EarnedGold { get; private set; }
        public bool IsOver => Phase == FieldPhase.Won || Phase == FieldPhase.Lost;

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
            if (Time > FieldConst.TimeLimit)
            {
                TimedOut = true;
                Phase = FieldPhase.Lost;
                Emit(FieldEventKind.Info, null, null, 0, false, "시간 초과...");
                return;
            }

            for (int i = 0; i < all.Count; i++) if (all[i].Alive) TickTimers(all[i], dt);
            for (int i = 0; i < all.Count; i++) if (all[i].Alive) Think(all[i], dt);
            Separate();
            CheckEnd();
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
            var ids = Stage.Waves[index];
            for (int i = 0; i < ids.Length; i++)
            {
                var fe = FieldData.Enemy(ids[i]);
                var eb = GameData.Enemy(ids[i]);
                var c = new Combatant(eb.Name, eb.Stats, false, eb);
                var u = new FieldUnit(c, false, fe.Strategy, EnemyRole(fe.Strategy), fe.Mov, fe.Range,
                    LookupSkills(fe.Skills), null, eb);
                float yFrac = (i + 1f) / (ids.Length + 1f);
                u.Pos = new Vector2(FieldConst.EnemyStartX + (i % 2) * 1f, FieldConst.Height * yFrac);
                u.ActionTimer = Interval(u) * (float)rng.NextDouble() * 0.5f;
                Enemies.Add(u);
            }

            all.Clear();
            all.AddRange(Players);
            all.AddRange(Enemies);
            Phase = FieldPhase.Running;
            Emit(FieldEventKind.Wave, null, null, index + 1, false, $"[{index + 1}/{WaveCount}] 적이 나타났다!");
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
            if (!AllDead(Enemies)) return;

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
                u.Cooldowns[i] = s.Cooldown;
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

            switch (s.Effect)
            {
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
            var r = Damage.Compute(src.C, tgt.C, coef, bonusCrit, rng, src.BuffTimer > 0f ? src.BuffMul : 1f);
            tgt.C.Hp = Mathf.Max(0, tgt.C.Hp - r.Amount);
            tgt.DamageTaken += r.Amount;
            src.DamageDealt += r.Amount;
            Emit(FieldEventKind.Hit, src, tgt, r.Amount, r.Crit, label);
            if (!tgt.Alive) Emit(FieldEventKind.Death, src, tgt, 0, false, $"{tgt.Name}이(가) 쓰러졌다.");
        }

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
