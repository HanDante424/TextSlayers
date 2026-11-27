using System.Collections.Generic;
using UnityEngine;

namespace TextSlayers
{
    public static partial class GameView
    {
        // 도트 캐릭터 머리 위 스킬 쿨타임 칸 3개 (사용자 2026-11-27).
        // 사용하면 짙은 회색 → 밝은 회색이 아래에서 차오름 → 다 차면 흰색 점멸 → 흰색(사용 가능).

        const int CooldownSlots = 3;
        const float CooldownSize = 9f, CooldownGap = 3f;        // 칸 크기 · 간격 [임시]
        const float ReadyBlinkTime = 0.45f, ReadyBlinkStep = 0.075f;   // 다 찼을 때 점멸 [임시]

        static readonly Color CooldownBorder = new Color(0.05f, 0.05f, 0.06f, 0.9f);
        static readonly Color CooldownEmpty = new Color(0.22f, 0.22f, 0.24f);   // 짙은 회색
        static readonly Color CooldownFill = new Color(0.66f, 0.66f, 0.69f);    // 밝은 회색
        static readonly Color CooldownReady = Color.white;
        static readonly Color CooldownNone = new Color(0.13f, 0.13f, 0.15f, 0.7f);   // 스킬이 없는 칸

        // 방금 다 찬 칸을 알기 위한 화면 쪽 기억
        static readonly Dictionary<(FieldUnit, int), (float last, float readyAt)> cooldownSeen = new Dictionary<(FieldUnit, int), (float, float)>();
        static FieldBattle cooldownBattle;

        static float CooldownRowHeight => CooldownSize + 2f;

        static void DrawCooldownSlots(FieldBattle battle, FieldUnit u, float centerX, float top, float now)
        {
            if (cooldownBattle != battle) { cooldownBattle = battle; cooldownSeen.Clear(); }
            float width = CooldownSlots * CooldownSize + (CooldownSlots - 1) * CooldownGap;
            for (int i = 0; i < CooldownSlots; i++)
            {
                var rect = new Rect(Mathf.Round(centerX - width * 0.5f + i * (CooldownSize + CooldownGap)), Mathf.Round(top), CooldownSize, CooldownSize);
                Fill(new Rect(rect.x - 1, rect.y - 1, rect.width + 2, rect.height + 2), CooldownBorder);
                if (i >= u.Skills.Length || u.Skills[i] == null || i >= u.Cooldowns.Length)
                {
                    Fill(rect, CooldownNone);
                    continue;
                }

                float left = u.Cooldowns[i];
                var key = (u, i);
                if (!cooldownSeen.TryGetValue(key, out var seen)) seen = (left, -10f);
                if (seen.last > 0f && left <= 0f) seen.readyAt = now;
                seen.last = left;
                cooldownSeen[key] = seen;

                if (left <= 0f)
                {
                    float since = now - seen.readyAt;
                    bool dim = since >= 0f && since < ReadyBlinkTime && (int)(since / ReadyBlinkStep) % 2 == 1;
                    Fill(rect, dim ? CooldownFill : CooldownReady);
                    continue;
                }
                float total = Mathf.Max(0.01f, u.Skills[i].EffectiveCooldown);
                float filled = Mathf.Round(rect.height * Mathf.Clamp01(1f - left / total));
                Fill(rect, CooldownEmpty);
                if (filled > 0f) Fill(new Rect(rect.x, rect.yMax - filled, rect.width, filled), CooldownFill);
            }
        }
    }
}
