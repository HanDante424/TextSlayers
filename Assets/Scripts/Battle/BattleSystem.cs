using UnityEngine;

namespace TextSlayers
{
    public enum BattlePhase
    {
        Running,    // 게이지가 차는 중
        PlayerTurn, // 에른 게이지가 참 → 시간 정지, 행동 선택 대기
        WaveClear,  // 적 처치 후 다음 웨이브 대기
        Won,
        Lost,
    }

    /// <summary>
    /// 스테이지 1회 전투 (대기형 ATB, 기획 v0.1 5장). MonoBehaviour 가 아니며 GameRoot 가 Tick 을 부른다.
    /// </summary>
    public sealed class BattleSystem
    {
        public readonly StageDef Stage;
        public readonly Combatant Player;
        public Combatant Enemy { get; private set; }
        public BattlePhase Phase { get; private set; }
        public int WaveIndex { get; private set; }
        public int WaveCount => Stage.Waves.Length;
        public bool Retreated { get; private set; }

        public int EarnedExp { get; private set; }
        public int EarnedGold { get; private set; }

        public bool IsOver => Phase == BattlePhase.Won || Phase == BattlePhase.Lost;

        readonly TextLog log;
        readonly System.Random rng;
        float waveTimer;

        public BattleSystem(StageDef stage, int playerLevel, TextLog log, int seed)
        {
            Stage = stage;
            this.log = log;
            rng = new System.Random(seed);
            Player = new Combatant("에른", GameData.ErnStatsAt(playerLevel), true);

            log.Add($"── {stage.Name} ──");
            log.Add(stage.Intro);
            SpawnWave(0);
        }

        void SpawnWave(int index)
        {
            WaveIndex = index;
            var def = GameData.Enemy(Stage.Waves[index]);
            Enemy = new Combatant(def.Name, def.Stats, false, def);
            Player.Gauge = 0f;
            Phase = BattlePhase.Running;
            log.Add($"[{index + 1}/{WaveCount}] {def.AppearText}", def.IsBoss ? LogKind.Warning : LogKind.System);
        }

        public void Tick(float dt)
        {
            if (Phase == BattlePhase.WaveClear)
            {
                waveTimer -= dt;
                if (waveTimer <= 0f) SpawnWave(WaveIndex + 1);
                return;
            }
            if (Phase != BattlePhase.Running) return;

            Player.Gauge = Mathf.Min(GameData.GaugeMax, Player.Gauge + Player.Stats.SPD * GameData.GaugePerSpd * dt);
            Enemy.Gauge = Mathf.Min(GameData.GaugeMax, Enemy.Gauge + Enemy.Stats.SPD * GameData.GaugePerSpd * dt);

            // 같은 프레임에 둘 다 차면 적이 먼저 행동한다 [임시]
            if (Enemy.Gauge >= GameData.GaugeMax)
            {
                Enemy.Gauge = 0f;
                EnemyAct();
                if (IsOver) return;
            }
            if (Player.Gauge >= GameData.GaugeMax)
            {
                Phase = BattlePhase.PlayerTurn;
            }
        }

        public bool CanUse(SkillDef skill) => Phase == BattlePhase.PlayerTurn && Player.Mp >= skill.MpCost;

        public void UseSkill(SkillDef skill)
        {
            if (Phase != BattlePhase.PlayerTurn) return;
            if (Player.Mp < skill.MpCost)
            {
                log.Add($"MP가 부족하다. ({skill.Name}: MP {skill.MpCost} 필요)", LogKind.Warning);
                return;
            }

            Player.Mp -= skill.MpCost;
            Player.Gauge = 0f;

            if (skill.Kind == ActionKind.Guard)
            {
                Player.Guarding = true;
                Player.Mp = Mathf.Min(Player.Stats.MP, Player.Mp + skill.MpGain);
                log.Add("에른이 검을 세워 방어 자세를 취한다.", LogKind.Player);
            }
            else
            {
                bool multi = skill.Hits > 1;
                if (multi) log.Add($"에른의 {skill.Name}!", LogKind.Player);
                for (int h = 0; h < skill.Hits && Enemy.Alive; h++)
                {
                    var r = Damage.Compute(Player, Enemy, skill.Coef, skill.BonusCrit, rng);
                    Enemy.Hp = Mathf.Max(0, Enemy.Hp - r.Amount);
                    string head = multi ? $"  {h + 1}타" : $"에른의 {skill.Name}";
                    log.Add($"{head}! {Enemy.Name}에게 {r.Amount} 피해{CritText(r)}", LogKind.Player);
                }
            }

            Player.Mp = Mathf.Min(Player.Stats.MP, Player.Mp + GameData.MpRegenPerAction);

            if (!Enemy.Alive) OnEnemyDefeated();
            else Phase = BattlePhase.Running;
        }

        /// <summary>전투 포기. 패배와 같은 보상 비율.</summary>
        public void Retreat()
        {
            if (IsOver) return;
            Retreated = true;
            Phase = BattlePhase.Lost;
            log.Add("에른이 숲에서 물러났다.", LogKind.Warning);
        }

        void EnemyAct()
        {
            float coef = 1f;
            string actName = "공격";

            if (Enemy.Charging)
            {
                Enemy.Charging = false;
                coef = GameData.BossHeavyCoef;
                actName = "강타";
            }
            else if (Enemy.Def.IsBoss && rng.NextDouble() < GameData.BossHeavyChance)
            {
                // 한 행동을 써서 강타를 예고 → 다음 행동이 강타
                Enemy.Charging = true;
                log.Add($"{Enemy.Name}이(가) 무기를 크게 치켜든다! (다음 행동: 강타)", LogKind.Warning);
                return;
            }

            var r = Damage.Compute(Enemy, Player, coef, 0f, rng);
            Player.Guarding = false; // 방어는 한 번 맞으면 풀림
            Player.Hp = Mathf.Max(0, Player.Hp - r.Amount);
            string guard = r.Guarded ? " (방어)" : "";
            log.Add($"{Enemy.Name}의 {actName}! 에른에게 {r.Amount} 피해{guard}{CritText(r)}", LogKind.Enemy);

            if (!Player.Alive)
            {
                Phase = BattlePhase.Lost;
                log.Add("에른이 쓰러졌다...", LogKind.Warning);
            }
        }

        void OnEnemyDefeated()
        {
            var def = Enemy.Def;
            EarnedExp += def.Exp;
            EarnedGold += def.Gold;
            log.Add($"{def.Name}을(를) 쓰러뜨렸다. 경험치 +{def.Exp}, 골드 +{def.Gold}", LogKind.Reward);

            if (WaveIndex + 1 >= WaveCount)
            {
                Phase = BattlePhase.Won;
                log.Add($"{Stage.Name} 클리어!", LogKind.Reward);
            }
            else
            {
                Phase = BattlePhase.WaveClear;
                waveTimer = GameData.WaveInterval;
            }
        }

        /// <summary>전투가 끝난 뒤 실제로 받는 보상. 패배 · 후퇴는 DefeatRewardRate, 메모리 없음.</summary>
        public void GetFinalRewards(out int exp, out int gold, out int memory)
        {
            if (Phase == BattlePhase.Won)
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

        static string CritText(HitResult r) => r.Crit ? " <b>치명타!</b>" : "";
    }
}
