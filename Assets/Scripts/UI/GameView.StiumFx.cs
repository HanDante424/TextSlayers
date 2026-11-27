using System.Collections.Generic;
using UnityEngine;

namespace TextSlayers
{
    public static partial class GameView
    {
        // 스티움 전용 연출 (사용자 2026-11-27). 호의 값은 시트를 만든 값(stium_fx)과 같아야 대검이 궤적을 따라간다.
        // 좌표는 발 위치 기준 도트, 오른쪽을 볼 때. 각도는 화면 기준(0 = 오른쪽, 90 = 아래).
        const float StiumBodyY = -28f;
        const float SpinRadius = 108f, SpinStartAngle = -90f, SpinWind = 0.02f, SpinSweep = 0.28f, SpinSwordHold = 0.45f;
        const float SmashRadius = 112f, SmashFrom = -100f, SmashTo = 25f, SmashSweep = 0.09f;
        const float SmashImpactLead = 0.15f;   // 시전 이펙트 길이 - 이 값 = 타격 시각 (FieldBattle 의 BladeSlam 시전)
        const float WaveDustSpacing = 0.35f;   // 검기가 이만큼(칸) 갈 때마다 입자를 남긴다 [임시]

        static bool IsStium(FieldEffect e) => e.Source != null && e.Source.Def != null && e.Source.Def.Id == "stium";

        static float SmashSlamStart(FieldEffect e) => Mathf.Max(0f, e.Duration - SmashImpactLead - SmashSweep);

        /// <summary>몸 중심에서 angle 방향, 칼끝이 반지름 radius 에 닿는 대검.</summary>
        static void DrawStiumSwordAt(Vector2 feet, float pixel, float angle, float radius, float alpha, bool flip)
        {
            var sword = SwordTexture();
            if (sword == null || alpha <= 0f) return;
            if (flip) angle = 180f - angle;
            float size = CleaveSwordZoom * pixel;
            Vector2 center = feet + new Vector2(0f, StiumBodyY * pixel);
            DrawSwordSprite(sword, center + Polar(angle) * (radius * pixel - SwordReach * size), angle, size, alpha);
        }

        /// <summary>회전베기: 대검이 한 바퀴 돌고 지나간 자리에 궤적이 남는다.</summary>
        static void DrawStiumSpin(FieldEffect e, Vector2 center, float unit, float age)
        {
            Vector2 feet = Feet(center, unit);
            bool flip = e.Dir.x < 0f;
            DrawSheet("stiumSpin", feet, unit, age, Color.white, flip);
            float k = Mathf.Clamp01((age - SpinWind) / SpinSweep);
            float alpha = Mathf.Clamp01((SpinSwordHold - age) / 0.1f);
            DrawStiumSwordAt(feet, FieldPixelSize(unit), SpinStartAngle + 360f * k, SpinRadius, alpha, flip);
        }

        /// <summary>강격검: 대검을 들고 있다가 빠르게 내려찍는다. 내려찍는 순간부터 궤적이 남는다.</summary>
        static void DrawStiumSmashCast(FieldEffect e, Vector2 center, float unit, float age)
        {
            Vector2 feet = Feet(center, unit);
            bool flip = e.Dir.x < 0f;
            float start = SmashSlamStart(e);
            DrawSheet("stiumSmash", feet, unit, age - start, Color.white, flip);

            float k = Mathf.Clamp01((age - start) / SmashSweep);
            float angle = Mathf.Lerp(SmashFrom, SmashTo, k * k);
            if (age < start) angle += Mathf.Sin(age * 55f) * 2f;                      // 들고 있는 동안 힘이 실려 떨림
            float alpha = Mathf.Clamp01((e.Duration + 0.15f - age) / 0.1f);
            DrawStiumSwordAt(feet, FieldPixelSize(unit), angle, SmashRadius, alpha, flip);
        }

        /// <summary>강격검 타격: 보라 지면 폭발 + 검보라 불길 + 가운데 작은 어둠 기둥.</summary>
        static void DrawStiumImpact(Vector2 center, float unit, float age)
        {
            Vector2 feet = Feet(center, unit);
            DrawSheet("darkSlam", feet, unit, age, Color.white);
            DrawSheet("boomDark", feet, unit, age, Color.white);
            DrawSheet("darkPillar", feet, unit, age, Color.white);
        }

        // ───────── 어둠의 검기 ─────────

        struct WaveDust { public Vector2 Pos; public float Born; public bool Flip; }

        static readonly List<WaveDust> waveDusts = new List<WaveDust>();
        static readonly Dictionary<FieldProjectile, Vector2> waveLast = new Dictionary<FieldProjectile, Vector2>();
        static FieldBattle waveBattle;

        /// <summary>검기가 지나간 자리의 입자. 검기가 사라진 뒤에도 잠시 남는다.</summary>
        static void DrawWaveRemnants(FieldBattle b, float unit, System.Func<Vector2, Vector2> P)
        {
            if (waveBattle != b) { waveBattle = b; waveDusts.Clear(); waveLast.Clear(); }
            foreach (var p in b.Projectiles)
            {
                if (p.Delay > 0f || p.Skill.Id != "stium_darkwave") continue;
                if (!waveLast.TryGetValue(p, out var last)) { waveLast[p] = p.Pos; continue; }
                while (Vector2.Distance(last, p.Pos) >= WaveDustSpacing)
                {
                    last = Vector2.MoveTowards(last, p.Pos, WaveDustSpacing);
                    waveDusts.Add(new WaveDust { Pos = last, Born = b.Time, Flip = waveDusts.Count % 2 == 0 });
                }
                waveLast[p] = last;
            }
            float life = SheetTime("waveDust");
            waveDusts.RemoveAll(d => b.Time - d.Born >= life);
            if (waveLast.Count > 32) waveLast.Clear();
            foreach (var d in waveDusts) DrawSheet("waveDust", P(d.Pos), unit, b.Time - d.Born, Color.white, d.Flip);
        }
    }
}
