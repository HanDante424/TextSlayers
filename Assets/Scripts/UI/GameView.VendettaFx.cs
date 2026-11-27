using System.Collections.Generic;
using UnityEngine;

namespace TextSlayers
{
    public static partial class GameView
    {
        // 벤데타 연출 (사용자 2026-11-27)

        // 권총 도트 (Vendetta_Palameltin_64 를 좌우로 뒤집고 절반 크기로 줄인 것, 총구가 오른쪽)
        const float PistolHandX = 42f, PistolHandY = 43f;   // 벤데타 도트에서 손 위치 (64×64 기준, 오른쪽을 볼 때)
        const float PistolGripX = 6f, PistolGripY = 20f;    // 권총 도트에서 손잡이 위치

        const float PistolSpinX = 11f, PistolSpinY = 18f;   // 장전할 때 돌리는 축 (방아쇠울, 권총 도트 기준)
        const float RecoilTime = 0.18f, RecoilAngle = 30f;   // 발사 반동: 총구가 들렸다가 돌아오는 시간 · 각도 [임시]
        const float ReloadTurns = 4f, SpecialReloadTurns = 2f;   // 장전하는 동안 도는 바퀴 수 (기본 · 특수) [임시]

        /// <summary>권총이 지금 돌아간 각도 (오른쪽을 볼 때 기준, 음수 = 총구가 위로)와 장전 중인지.</summary>
        static float PistolAngle(FieldUnit u, float now, out bool reloading)
        {
            reloading = u.ReloadTimer > 0f;
            if (reloading)
            {
                float duration = u.LoadingSpecial ? FieldBattle.SpecialReloadTime : FieldBattle.ReloadTime;
                float k = 1f - u.ReloadTimer / duration;
                return -360f * (u.LoadingSpecial ? SpecialReloadTurns : ReloadTurns) * k;
            }
            float since = now - u.LastShotTime;
            if (since < 0f || since >= RecoilTime) return 0f;
            float rest = 1f - since / RecoilTime;
            return -RecoilAngle * rest * rest;
        }

        static void DrawVendettaPistol(Rect body, bool faceLeft, FieldUnit u, float now)
        {
            var texture = WeaponTexture("vendetta_pistol");
            if (texture == null) return;
            float pixel = body.width / FieldSpriteCanvasSize;
            float handX = faceLeft ? FieldSpriteCanvasSize - PistolHandX : PistolHandX;
            float gripX = faceLeft ? FieldSpriteCanvasSize - PistolGripX : PistolGripX;
            var rect = new Rect(body.x + (handX - gripX) * pixel, body.y + (PistolHandY - PistolGripY) * pixel, body.width, body.height);
            float angle = PistolAngle(u, now, out bool reloading);
            if (angle == 0f)
            {
                PaintFieldSprite(rect, texture, faceLeft, Color.white);
                return;
            }
            // 반동은 손잡이를, 장전 회전은 방아쇠울을 축으로
            float axisX = reloading ? PistolSpinX : PistolGripX, axisY = reloading ? PistolSpinY : PistolGripY;
            if (faceLeft) axisX = FieldSpriteCanvasSize - axisX;
            Vector3 pivot = new Vector2(rect.x + axisX * pixel, rect.y + axisY * pixel);
            var previous = GUI.matrix;
            GUI.matrix = previous * Matrix4x4.TRS(pivot, Quaternion.Euler(0f, 0f, faceLeft ? -angle : angle), Vector3.one)
                                  * Matrix4x4.TRS(-pivot, Quaternion.identity, Vector3.one);
            PaintFieldSprite(rect, texture, faceLeft, Color.white);
            GUI.matrix = previous;
        }

        // 무기 케이스 도트 (Vendetta_Weapon_Case_64_Flat). 케이스 몸통 중심과 높이 (64×64 도트 기준)
        const float CaseCenterX = 32.5f, CaseCenterY = 35.5f, CaseHeight = 48f;
        const float CaseBackX = 18f, CaseBackY = 34f;     // 등에 멜 때 케이스 중심 (벤데타 도트 64×64 기준, 오른쪽을 볼 때)
        const float CaseBackTilt = 14f;                   // 등에 비스듬히 [임시]

        /// <summary>등에 멘 케이스 (몸 뒤에 그린다). 그렸으면 true.</summary>
        static bool DrawBackCase(Rect body, bool faceLeft)
        {
            var texture = WeaponTexture("vendetta_case");
            if (texture == null) return false;
            if (Event.current.type != EventType.Repaint) return true;
            float pixel = body.width / FieldSpriteCanvasSize;
            float x = faceLeft ? FieldSpriteCanvasSize - CaseBackX : CaseBackX;
            var center = new Vector2(body.x + x * pixel, body.y + CaseBackY * pixel);
            DrawCaseSprite(texture, center, pixel, faceLeft ? CaseBackTilt : -CaseBackTilt, 1f);
            return true;
        }

        /// <summary>케이스 몸통 중심을 center 에 두고 angle 만큼 돌려 그린다.</summary>
        static void DrawCaseSprite(Texture2D texture, Vector2 center, float pixel, float angle, float alpha)
        {
            Vector2 offset = new Vector2(32f - CaseCenterX, 32f - CaseCenterY) * pixel;   // 캔버스 중심까지
            float rad = angle * Mathf.Deg2Rad, c = Mathf.Cos(rad), n = Mathf.Sin(rad);
            var turned = new Vector2(offset.x * c - offset.y * n, offset.x * n + offset.y * c);
            float size = FieldSpriteCanvasSize * pixel;
            DrawFxRect(texture, center + turned, size, size, Color.white, alpha, angle);
        }

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
