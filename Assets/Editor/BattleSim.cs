using System.Text;
using UnityEditor;
using UnityEngine;

namespace TextSlayers.EditorTools
{
    /// <summary>
    /// 전투 자동 시뮬레이션 (밸런스 · 예외 확인용). 실제 BattleSystem 을 그대로 돌린다.
    /// 메뉴: TextSlayers / 전투 시뮬레이션
    /// 배치 모드: -executeMethod TextSlayers.EditorTools.BattleSim.Run
    /// 자동 플레이 규칙 [임시]: 보스 강타 준비 → 방어, MP 14↑ → 파이어 블레이드, 8↑ → 트리플 슬래시, 그 외 베기.
    /// </summary>
    public static class BattleSim
    {
        const int RunsPerLevel = 300;
        const int MaxLevelToTest = 6;
        const float Dt = 1f / 60f;
        const int MaxTicks = 60 * 60 * 10; // 10분 넘으면 멈춘 것으로 간주

        [MenuItem("TextSlayers/전투 시뮬레이션")]
        public static void Run()
        {
            var sb = new StringBuilder("[TextSlayers] 전투 시뮬레이션\n");
            foreach (var stage in GameData.Stages)
            {
                for (int lv = 1; lv <= MaxLevelToTest; lv++)
                {
                    int wins = 0, stuck = 0;
                    float hpLeft = 0f, seconds = 0f, wavesReached = 0f;
                    for (int i = 0; i < RunsPerLevel; i++)
                    {
                        var b = new BattleSystem(stage, lv, new TextLog(), 1000 + i);
                        int ticks = 0;
                        while (!b.IsOver && ticks < MaxTicks)
                        {
                            if (b.Phase == BattlePhase.PlayerTurn) b.UseSkill(Choose(b));
                            else { b.Tick(Dt); ticks++; }
                        }
                        if (!b.IsOver) stuck++;
                        if (b.Phase == BattlePhase.Won) { wins++; hpLeft += b.Player.HpRatio; }
                        seconds += ticks * Dt;
                        wavesReached += b.WaveIndex + 1;
                    }
                    sb.AppendLine($"{stage.Name} Lv.{lv}: 승률 {100f * wins / RunsPerLevel:0.0}%  " +
                                  $"승리 시 남은 HP {(wins > 0 ? 100f * hpLeft / wins : 0f):0}%  " +
                                  $"평균 도달 웨이브 {wavesReached / RunsPerLevel:0.00}/{stage.Waves.Length}  " +
                                  $"평균 {seconds / RunsPerLevel:0}초  멈춤 {stuck}");
                }
            }
            Debug.Log(sb.ToString());
        }

        static SkillDef Choose(BattleSystem b)
        {
            SkillDef slash = Find("slash"), triple = Find("triple_slash"), blade = Find("fire_blade"), guard = Find("guard");
            if (b.Enemy.Charging) return guard;
            if (b.Player.Mp >= blade.MpCost) return blade;
            if (b.Player.Mp >= triple.MpCost) return triple;
            return slash;
        }

        static SkillDef Find(string id)
        {
            foreach (var s in GameData.ErnSkills) if (s.Id == id) return s;
            return GameData.ErnSkills[0];
        }
    }
}
