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

        // ───────── 파동 · 손아귀 · 거두기 (사용자 2026-11-27) ─────────

        const float PulseHeartY = 54f;      // darkPulse 시트에서 폭발 중심이 기준점(바닥)보다 위에 있는 높이 (도트)
        const float GraspZoom = 2f;         // 손아귀 시트 배율: 반지름 4칸에 맞춤 [임시]
        const int HarvestOrbs = 5;
        const float HarvestGap = 0.06f, HarvestFlight = 0.75f, HarvestFeed = 0.5f;   // 알갱이 간격 · 비행 · 흡수 번쩍임 [임시]
        const float HarvestTrailTime = 0.22f;   // 꼬리에 남는 지난 시간 [임시]
        const int HarvestTrailSteps = 16;
        const float HarvestLength = (HarvestOrbs - 1) * HarvestGap + HarvestFlight + HarvestFeed;

        static readonly Color HarvestTrailHead = new Color(0.77f, 0.55f, 1f), HarvestTrailTail = new Color(0.29f, 0.11f, 0.48f);

        /// <summary>어둠의 파동인가 (돌파 등 다른 퍼지는 원과 구분).</summary>
        static bool IsStiumPulse(FieldEffect e) => IsStium(e) && e.Radius >= 2.4f && e.Color == SkillColor.Dark;

        /// <summary>어둠의 파동: 가운데 구가 없는 어둠 폭발. 폭발 중심을 몸 가운데에 맞춘다.</summary>
        static void DrawStiumPulse(Vector2 center, float unit, float age)
        {
            float pixel = FieldPixelSize(unit);
            Vector2 at = Feet(center, unit) + new Vector2(0f, (PulseHeartY + StiumBodyY) * pixel);
            DrawSheet("darkPulse", at, unit, age, Color.white);
        }

        /// <summary>어둠의 손아귀: 범위의 어둠이 중심으로 빨려 든다 (유닛 아래에 그린다).</summary>
        static void DrawStiumGrasp(Vector2 center, float unit, float age)
            => DrawSheet("grasp", center, unit, age, Color.white, false, false, 0f, GraspZoom);

        /// <summary>어둠 거두기: 대상의 뒤로 알갱이가 빠져나와, 궤적을 남기며 크게 돌아 스티움에게 빨려 든다.</summary>
        static void DrawStiumHarvest(FieldEffect e, Vector2 caster, float unit, float age, System.Func<Vector2, Vector2> P)
        {
            if (e.Points == null) return;
            float pixel = FieldPixelSize(unit);
            for (int i = 0; i < e.Points.Length; i++)
            {
                Vector2 target = P(e.Points[i]);
                Vector2 back = target - caster;                      // 스티움 반대쪽 = 대상의 뒤
                back = back.sqrMagnitude > 0.01f ? back.normalized : Vector2.up;
                DrawSheet("stealStart", target, unit, age, Color.white);
                for (int k = 0; k < HarvestOrbs; k++)
                {
                    float time = age - k * HarvestGap;
                    if (time <= 0f || time >= HarvestFlight + HarvestTrailTime) continue;
                    int seed = i * 11 + k;
                    Vector2 At(float t) => HarvestOrbAt(e, seed, k, target, back, caster, pixel, t / HarvestFlight);

                    // 궤적: 지나온 길을 머리에서 꼬리로 갈수록 가늘고 어둡게. 도착한 뒤에는 꼬리가 마저 빨려 든다
                    Vector2 previous = At(Mathf.Min(time, HarvestFlight));
                    for (int n = 1; n <= HarvestTrailSteps; n++)
                    {
                        float back01 = n / (float)HarvestTrailSteps;
                        float t = time - back01 * HarvestTrailTime;
                        if (t <= 0f) break;
                        Vector2 point = At(Mathf.Min(t, HarvestFlight));
                        if ((point - previous).sqrMagnitude > 0.25f)
                        {
                            Color color = Color.Lerp(HarvestTrailHead, HarvestTrailTail, back01);
                            DrawLine(point, previous, Mathf.Max(1f, 4f * (1f - back01)) * pixel * 2f, color, 0.95f * (1f - back01 * 0.7f));
                        }
                        previous = point;
                    }

                    if (time < HarvestFlight)                        // 머리
                    {
                        Vector2 head = At(time), before = At(Mathf.Max(0f, time - 0.02f));
                        float angle = (head - before).sqrMagnitude > 0.01f ? AngleOf(head - before) : AngleOf(back);
                        DrawSheet("stealOrb", head, unit, age + k * 0.03f, Color.white, false, true, angle);
                    }
                }
            }
            for (int k = 0; k < HarvestOrbs; k++)       // 알갱이가 닿을 때마다 시전자에게서 번쩍
                DrawSheet("stealFeed", caster, unit, age - (k * HarvestGap + HarvestFlight), Color.white);
        }

        /// <summary>
        /// 알갱이 위치 (3차 곡선): 대상에서 뒤쪽으로 튀어나가 → 옆으로 크게 돌아 → 시전자. 처음은 천천히, 끝은 빠르게.
        /// </summary>
        static Vector2 HarvestOrbAt(FieldEffect e, int seed, int k, Vector2 target, Vector2 back, Vector2 caster, float pixel, float u)
        {
            u = Mathf.Clamp01(u);
            u = u * (0.45f + 0.55f * u);                             // 뒤로 갈수록 빨라진다
            float spread = (k - (HarvestOrbs - 1) * 0.5f) * 28f + (Hash(e.Start, seed) - 0.5f) * 16f;   // 뒤쪽 부채꼴
            float side = spread >= 0f ? 1f : -1f;
            Vector2 out1 = Polar(AngleOf(back) + spread);
            Vector2 normal = new Vector2(-back.y, back.x);

            Vector2 c1 = target + out1 * ((120f + Hash(e.Start, seed + 31) * 70f) * pixel);
            Vector2 c2 = Vector2.Lerp(target, caster, 0.55f) + normal * (side * (90f + Hash(e.Start, seed + 57) * 80f) * pixel);
            float v = 1f - u;
            return target * (v * v * v) + c1 * (3f * v * v * u) + c2 * (3f * v * u * u) + caster * (u * u * u);
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
