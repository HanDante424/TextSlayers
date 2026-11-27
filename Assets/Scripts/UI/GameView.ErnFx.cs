using UnityEngine;

namespace TextSlayers
{
    public static partial class GameView
    {
        // 에른 스킬 연출 (사용자 2026-11-27)

        const float TubeLinger = 0.45f, TubeFade = 0.25f;   // 도착 뒤 화염 관이 남는 시간 · 사라지는 시간 [임시]
        const float TubeSegment = 56f;                       // 관 한 마디 길이 (도트). 시트의 TUBE 와 같아야 이어진다

        // 파이어 블레이드: 기본 공격 궤적을 넓게
        static readonly CleaveStyle FireBladeCleave = new CleaveStyle { Sheet = "fireWide", CenterX = -30f, Radius = 168f, Wind = 0.03f, Sweep = 0.13f, WeaponTime = 0.45f, WeaponSprite = "ern_sword_flame", WeaponZoom = 2f };   // [임시]

        static bool IsErnSkill(FieldEffect e, string id) => e.SkillId == id && e.Source != null;

        /// <summary>에른 스킬 이펙트의 화면 길이. 해당 없으면 0.</summary>
        static float ErnEffectLength(FieldEffect e)
        {
            if (e.Kind == FieldEffectKind.Pulse && IsErnSkill(e, "ern_fire_buster")) return SheetTime("ernBuster");
            if (e.Kind == FieldEffectKind.Trail && IsErnSkill(e, "ern_fire_stream"))
                return e.Duration + TubeLinger + TubeFade;
            if (e.Kind == FieldEffectKind.BigSlash && IsErnSkill(e, "ern_fire_blade")) return SheetTime(FireBladeCleave.Sheet);
            if (e.Kind == FieldEffectKind.Multi && IsErnSkill(e, "ern_fire_blade")) return SheetTime("flameSlashes");
            if (e.Kind == FieldEffectKind.Whirl && IsErnSkill(e, "ern_fire_execution")) return e.Duration + SheetTime(ErnCleave.Sheet);
            return 0f;
        }

        /// <summary>에른 스킬이면 그리고 true.</summary>
        static bool DrawErnEffect(FieldEffect e, Vector2 c, Vector2 follow, float unit, float U, float age, System.Func<Vector2, Vector2> P)
        {
            if (e.Kind == FieldEffectKind.Pulse && IsErnSkill(e, "ern_fire_buster"))      // 큰 폭발 + 퍼지는 화염파
            {
                DrawSheet("ernBuster", Feet(c, unit), unit, age, Color.white);
                return true;
            }
            if (e.Kind == FieldEffectKind.Burst && IsErnSkill(e, "ern_fire_buster")) return true;   // 위 시트에 포함
            if (e.Kind == FieldEffectKind.Trail && IsErnSkill(e, "ern_fire_stream"))
            {
                DrawFireTube(e, unit, age, P, true);      // 앞면 (뒷면은 유닛 아래: DrawGroundEffects)
                return true;
            }
            if (e.Kind == FieldEffectKind.BigSlash && IsErnSkill(e, "ern_fire_blade"))    // 넓은 화염 궤적
            {
                DrawCleave(FireBladeCleave, e.Dir, follow + e.Dir * (unit * SwingReachAhead), unit, U * WeaponScale, age, SkillColor.Fire);
                return true;
            }
            if (e.Kind == FieldEffectKind.Multi && IsErnSkill(e, "ern_fire_blade"))       // 궤적 안의 대상에게 화염 참격 다발
            {
                DrawSheet("flameSlashes", c, unit, age, Color.white);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 파이어 스트림: 이동 경로에 가로로 누운 화염 폭풍의 관이 깔리고 그 안을 타고 지나간다 (사용자 2026-11-27).
        /// 마디는 에른이 지나가는 순서대로 생긴다.
        /// </summary>
        static void DrawFireTube(FieldEffect e, float unit, float age, System.Func<Vector2, Vector2> P, bool front)
        {
            float travel = Mathf.Max(0.05f, e.Duration);
            Vector2 from = P(e.Origin), to = P(e.End), path = to - from;
            float length = path.magnitude;
            if (length < 1f) return;
            float angle = AngleOf(path);
            float step = TubeSegment * FieldPixelSize(unit);
            int count = Mathf.Max(1, Mathf.CeilToInt(length / step));
            float fade = Mathf.Clamp01((travel + TubeLinger + TubeFade - age) / TubeFade);
            if (fade <= 0f) return;
            var tint = new Color(1f, 1f, 1f, fade);
            for (int k = 0; k <= count; k++)
            {
                float at = k / (float)count;
                if (age < travel * at - 0.04f) break;
                DrawSheet(front ? "fireTubeFront" : "fireTubeBack", from + path * at, unit, age, tint, false, true, angle);
            }
        }

        /// <summary>파이어 엑스큐션: 지속시간 동안 타수만큼 재빠르게 휘두르며 사방에 화염 궤적.</summary>
        static void DrawFireExecution(FieldEffect e, Vector2 follow, float unit, float U, float age)
        {
            int swings = Mathf.Max(1, e.Count);
            float step = e.Duration / swings;
            for (int k = 0; k < swings; k++)
            {
                float local = age - k * step;
                if (local < 0f || local >= SheetTime(ErnCleave.Sheet)) continue;
                // 직전 칼질이 남긴 무기는 지우고 잔흔만 남긴다
                Vector2 dir = Polar(k * 137.5f + Hash(e.Start, k) * 30f);
                DrawCleave(ErnCleave, dir, follow + dir * (unit * SwingReachAhead), unit, U * WeaponScale, local, SkillColor.Fire,
                           local < step || k == swings - 1);
            }
        }
    }
}
