using System.Collections.Generic;
using UnityEngine;

namespace TextSlayers
{
    /// <summary>
    /// 팀 기여도 그래프 (사용자 2026-11-28): 카드 열의 '그래프' 버튼을 켜면 캐릭터 카드마다 옆에
    /// 딜(빨강) · 힐(초록) · 탱(노랑) 3줄이 붙어 팀 전원을 한눈에 비교한다.
    /// </summary>
    public static partial class GameView
    {
        // ── 카드 옆 그래프 칸 [임시] ──
        const float TeamStripW = 128f;               // 그래프를 켤 때 열이 넓어지는 폭 (카드와의 간격 포함)
        const float TeamStripGap = 4f;
        const float TeamStripPad = 4f, TeamBarGap = 4f;

        sealed class Metric
        {
            public string Short, Name;
            public Color Color;
            public System.Func<FieldUnit, int> Value;
        }

        // 순서: 딜 · 힐 · 탱. 색은 흰 글자가 읽히게 조금 어둡게 [임시]
        static readonly Metric[] Metrics =
        {
            new Metric { Short = "딜", Name = "준 피해", Color = new Color(0.78f, 0.22f, 0.2f), Value = u => u.DamageDealt },
            new Metric { Short = "힐", Name = "회복", Color = new Color(0.2f, 0.62f, 0.28f), Value = u => u.HealDone },
            new Metric { Short = "탱", Name = "받은 피해", Color = new Color(0.74f, 0.58f, 0.08f), Value = u => u.DamageTaken },
        };

        static bool allyGraph, foeGraph;             // 팀별로 켜고 끈다. 다음 전투에도 유지

        static void TeamTotals(List<FieldUnit> team, Metric m, out int max, out int total)
        {
            max = 0; total = 0;
            foreach (var u in team)
            {
                int v = m.Value(u);
                total += v;
                if (v > max) max = v;
            }
        }

        static string Share(int value, int total) => total > 0 ? $"{value * 100f / total:0}%" : "0%";

        static void DrawMeter(Rect r, int value, int max, Color color)
        {
            Fill(r, BarBack);
            if (max > 0 && value > 0) Fill(new Rect(r.x, r.y, r.width * value / max, r.height), color);
        }

        /// <summary>그래프 칸 위의 색 안내: 딜 · 힐 · 탱.</summary>
        static void DrawTeamLegend(Rect r)
        {
            float w = r.width / Metrics.Length;
            for (int k = 0; k < Metrics.Length; k++)
            {
                var cell = new Rect(r.x + k * w + 2f, r.y + 2f, w - 4f, r.height - 4f);
                Fill(cell, Metrics[k].Color);
                GUI.Label(cell, $"<size=13><b>{Metrics[k].Short}</b></size>", barText);
            }
        }

        /// <summary>카드 한 장 옆의 3줄. 막대 길이는 팀 최고값 기준, 글자는 수치와 팀 합계에서 차지하는 몫.</summary>
        static void DrawTeamStrip(Rect r, FieldUnit u, int[] maxes, int[] totals)
        {
            Fill(r, CardNameBack);
            float h = (r.height - TeamStripPad * 2 - TeamBarGap * (Metrics.Length - 1)) / Metrics.Length;
            for (int k = 0; k < Metrics.Length; k++)
            {
                var bar = new Rect(r.x + TeamStripPad, r.y + TeamStripPad + k * (h + TeamBarGap), r.width - TeamStripPad * 2, h);
                int value = Metrics[k].Value(u);
                DrawMeter(bar, value, maxes[k], Metrics[k].Color);
                // 값이 0 이어도 어느 줄인지 알 수 있게 왼쪽 끝에 색 표시
                Fill(new Rect(bar.x, bar.y, 3f, bar.height), Metrics[k].Color);
                GUI.Label(bar, $"<size=13>{value}  <color=#dddddd>{Share(value, totals[k])}</color></size>", barText);
            }
        }
    }
}
