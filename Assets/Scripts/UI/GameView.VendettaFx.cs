using System.Collections.Generic;
using UnityEngine;

namespace TextSlayers
{
    public static partial class GameView
    {
        // 벤데타 연출 (사용자 2026-11-27)

        const float ThrustSpacing = 0.032f;   // 이 시간 간격의 지난 위치마다 추진 연기 · 불씨 [임시]

        /// <summary>벤데타 전용 적중 시트. 없으면 null.</summary>
        static string VendettaBurstSheet(FieldEffect e)
        {
            switch (e.SkillId)
            {
                case "vd_incendiary": return "airBoom";        // 작열탄: 공중 폭발
                case "vd_bunkerbuster": return "airBoomBig";   // 벙커버스터: 더 큰 공중 폭발
                case "vd_pilebunker": return "pileImpact";     // 파일벙커: 앞으로 터져 나가는 물리 충격
                case "mel_fire_buster": return "airBoom";      // 멜 파이어 버스터: 작열탄과 같은 폭발
                default: return IsMelBasic(e) ? "iceSpray" : null;   // 멜 기본 공격: 얼음이 대상 뒤로 퍼짐
            }
        }

        static void DrawVendettaBurst(string key, Vector2 center, float unit, float age, Vector2 dir)
        {
            Vector2 at = center + new Vector2(0f, -unit * 0.3f);          // 대상 몸 높이
            float angle = (key == "pileImpact" || key == "iceSpray") && dir.sqrMagnitude > 0.001f ? AngleOf(dir) : 0f;
            DrawSheet(key, at, unit, age, Color.white, false, false, angle);
        }

        static readonly Dictionary<(FieldEffect, int), Vector2> missileHits = new Dictionary<(FieldEffect, int), Vector2>();

        /// <summary>미사일이 향하는 곳: 살아 있는 대상의 지금 위치. 착탄한 뒤에는 그 자리에 고정.</summary>
        static Vector2 MissileTarget(FieldEffect e, int k, bool landed, System.Func<Vector2, Vector2> P)
        {
            if (landed && missileHits.TryGetValue((e, k), out var fixedAt)) return fixedAt;
            var unit = e.Units != null && k < e.Units.Length ? e.Units[k] : null;
            Vector2 at = P(unit != null && unit.Alive ? unit.Pos : e.Points[k]);
            if (landed)
            {
                if (missileHits.Count > 200) missileHits.Clear();
                missileHits[(e, k)] = at;
            }
            return at;
        }

        /// <summary>발사 후 since 초일 때의 위치: 곧게 상승 → 대상에게 곡선으로 내리꽂힘.</summary>
        static Vector2 MissileAt(float since, Rect field, float unit, Vector2 mouth, Vector2 apex, Vector2 hit)
        {
            if (since < FxMissileRise) return Vector2.Lerp(mouth, apex, Ease(Mathf.Clamp01(since / FxMissileRise)));
            float f = Mathf.Clamp01((since - FxMissileRise) / FxMissileFall);
            var ctrl = new Vector2((apex.x + hit.x) * 0.5f, Mathf.Max(field.y + unit * 0.2f, Mathf.Min(apex.y, hit.y) - unit * 1.5f));
            return Arc(apex, ctrl, hit, f * (0.6f + 0.4f * f));      // 뒤로 갈수록 빨라진다
        }

        static void DrawMissile(Rect field, float unit, float since, Vector2 mouth, Vector2 apex, Vector2 hit, Color main, Color light)
        {
            float flight = FxMissileRise + FxMissileFall;

            // 지나온 길에 남는 추진 연기와 불씨
            float life = SheetTime("thrust");
            int first = Mathf.Max(0, Mathf.CeilToInt((since - life) / ThrustSpacing));
            for (int n = first; n * ThrustSpacing <= Mathf.Min(since, flight); n++)
            {
                float t = n * ThrustSpacing;
                DrawSheet("thrust", MissileAt(t, field, unit, mouth, apex, hit), unit, since - t, Color.white, n % 2 == 0);
            }

            if (since < flight)
            {
                Vector2 pos = MissileAt(since, field, unit, mouth, apex, hit);
                Vector2 ahead = MissileAt(since + 0.02f, field, unit, mouth, apex, hit);
                float angle = (ahead - pos).sqrMagnitude > 0.01f ? AngleOf(ahead - pos) : -90f;
                DrawFx(fxDisc, pos, unit * 0.45f, light, 0.9f);
                DrawFxRect(Texture2D.whiteTexture, pos, unit * 0.42f, unit * 0.16f, Color.white, 1f, angle);   // 탄체
                if (since < FxMissileRise * 0.25f) DrawFx(fxDisc, mouth, unit * 0.9f, main, 1f - since / (FxMissileRise * 0.25f));   // 발사 섬광
                else if (since >= FxMissileRise) DrawFx(fxRing, hit, unit * 0.9f, new Color(1f, 0.3f, 0.2f), 0.5f);               // 조준 표시
            }
            else DrawSheet("boom", Feet(hit, unit), unit, since - flight, Color.white);
        }
    }
}
