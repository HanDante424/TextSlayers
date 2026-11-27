using UnityEngine;

namespace TextSlayers
{
    /// <summary>세부 연출 이펙트 (사용자 2026-11-26): 대검 · 십자가 · 미사일 · 얼음 · 잡기 · 연타 등. 전부 연출용 [임시].</summary>
    public static partial class GameView
    {
        const float FxBigSlash = 0.4f, FxCoin = 0.9f, FxSlantPillar = 0.5f, FxCrossSlam = 0.45f,
                    FxFan = 0.45f, FxLeapOff = 0.3f, FxBladeSwing = 0.4f, FxMissileGap = 0.08f, FxMissileRise = 0.4f,
                    FxMissileFall = 0.5f, FxMissileBoom = 0.3f, FxCasePlant = 0.35f, FxCrossFallDrop = 0.25f, FxCrossFallAfter = 0.4f;

        // 일식의 아가리 (사용자 2026-11-27): 벌림 → 빠르게 닫힘 → 닫힌 채 0.25초 → 0.25초 동안 사라짐
        const float MawOpen = 0.2f, MawClose = 0.06f, MawHold = 0.25f, MawFade = 0.25f;
        const float FxMaw = MawOpen + MawClose + MawHold + MawFade;

        static Texture2D fxDiamond;
        static Texture2D Diamond => fxDiamond != null ? fxDiamond
            : fxDiamond = MakeFxTex(64, (u, v) => Mathf.Clamp01((1f - Mathf.Abs(u) - Mathf.Abs(v)) * 10f));

        const float MissileRiseHeight = 3f;   // 다연장 탄두 수직 상승 높이 (칸) [임시]
        const float SkyCutTime = 0.14f;      // 하늘가르기: 내려베는 시간

        /// <summary>대검과 궤적이 같이 쓰는 각도: 머리 위(뒤로 살짝 젖힘) → 바라보는 방향 아래.</summary>
        static float SkyCutAngle(Vector2 dir, float age)
        {
            float side = dir.x >= 0f ? 1f : -1f;
            float k = Mathf.Clamp01(age / SkyCutTime);
            return AngleOf(dir) + side * Mathf.Lerp(-105f, 0f, k * k);   // 끝 각도 = 직선 잔흔 방향 (검과 잔흔이 맞게)
        }
        const float GhostLinger = 0.35f;     // 잔상이 남는 시간
        const int GhostCount = 8;

        const float FistSwingTime = 0.65f;   // 주먹 원호 · 균열 연출 시간 (착탄 시트는 더 길게 남는다)

        static float SpecialLength(FieldEffect e)
        {
            switch (e.Kind)
            {
                case FieldEffectKind.BladeSlam when IsStium(e) && e.Target != null:
                    return Mathf.Max(e.Duration, SmashSlamStart(e) + SheetTime("stiumSmash"));
                case FieldEffectKind.BigSlash: return FxBigSlash;
                case FieldEffectKind.Coin: return FxCoin;
                case FieldEffectKind.SlantPillar: return FxSlantPillar;
                case FieldEffectKind.CrossSlam: return FxCrossSlam;
                case FieldEffectKind.Maw: return FxMaw;
                case FieldEffectKind.FanWave: return FxFan;
                case FieldEffectKind.LeapOff: return FxLeapOff;
                case FieldEffectKind.FireLine: return 0.7f;
                case FieldEffectKind.Afterimage: return Mathf.Max(0.1f, e.Duration) + GhostLinger;
                case FieldEffectKind.Thrust: return 0.4f;
                case FieldEffectKind.FistSlam: return FistSwingTime + SheetTime("slam");
                case FieldEffectKind.SwordCut: return 0.4f;
                case FieldEffectKind.WhiteSlash: return 0.65f;
                case FieldEffectKind.IceShatter: return SheetTime("iceShatter");
                case FieldEffectKind.BladeSwing: return FxBladeSwing;
                case FieldEffectKind.Missiles: return FxCasePlant + Mathf.Max(1, e.Count) * FxMissileGap + FxMissileRise + FxMissileFall + Mathf.Max(FxMissileBoom, SheetTime("boom"));
                case FieldEffectKind.CrossFall: return e.Duration + FxCrossFallAfter;
                case FieldEffectKind.Landing: return e.Duration + SheetTime("landing");
                case FieldEffectKind.BladeSlam: return Mathf.Max(0.35f, e.Duration);
                default: return e.Duration > 0f ? e.Duration : FxDefaultHold;   // BladeRaise · Whirl · Freeze · Grab · Punches · CrossHold · ForwardSlashes
            }
        }

        static void DrawBlade(Vector2 pivot, float angleDeg, float length, float width, Color main, float alpha)
        {
            Vector2 dir = new Vector2(Mathf.Cos(angleDeg * Mathf.Deg2Rad), Mathf.Sin(angleDeg * Mathf.Deg2Rad));
            Vector2 mid = pivot + dir * (length * 0.5f);
            DrawFxRect(fxLine, mid, length * 1.15f, width * 2.2f, main, 0.5f * alpha, angleDeg);              // 광채
            DrawFxRect(Texture2D.whiteTexture, mid, length, width, Light(main), 0.95f * alpha, angleDeg);     // 직사각형 날
            DrawFxRect(Texture2D.whiteTexture, mid, length, width * 0.18f, Color.white, alpha, angleDeg);     // 날 선
            DrawFxRect(Texture2D.whiteTexture, pivot + dir * (length * 0.08f), width * 0.3f, width * 1.7f, main, alpha, angleDeg);   // 코등이
        }

        static void DrawCross(Vector2 c, float height, Color main, float alpha, float tiltDeg = 0f, bool inverted = false)
        {
            float w = height * 0.2f;
            Vector2 up = new Vector2(Mathf.Sin(tiltDeg * Mathf.Deg2Rad), -Mathf.Cos(tiltDeg * Mathf.Deg2Rad));
            float barAt = (inverted ? -0.2f : 0.2f) * height;
            DrawFxRect(fxDisc, c, height * 1.1f, height * 1.1f, main, 0.35f * alpha);
            DrawFxRect(Texture2D.whiteTexture, c, w, height, Light(main), alpha, tiltDeg);
            DrawFxRect(Texture2D.whiteTexture, c + up * barAt, height * 0.62f, w, Light(main), alpha, tiltDeg);
            DrawFxRect(Texture2D.whiteTexture, c, w * 0.3f, height * 0.9f, Color.white, alpha, tiltDeg);
        }

        /// <summary>필드 안의 점에서 dir 방향으로 필드 가장자리까지의 거리.</summary>
        static float DistanceToEdge(Rect field, Vector2 from, Vector2 dir)
        {
            float len = field.width + field.height;
            if (dir.x > 0.0001f) len = Mathf.Min(len, (field.xMax - from.x) / dir.x);
            if (dir.x < -0.0001f) len = Mathf.Min(len, (field.x - from.x) / dir.x);
            if (dir.y > 0.0001f) len = Mathf.Min(len, (field.yMax - from.y) / dir.y);
            if (dir.y < -0.0001f) len = Mathf.Min(len, (field.y - from.y) / dir.y);
            return Mathf.Max(0f, len);
        }

        static Vector2 Arc(Vector2 a, Vector2 ctrl, Vector2 b, float t)
            => Vector2.Lerp(Vector2.Lerp(a, ctrl, t), Vector2.Lerp(ctrl, b, t), t);

        /// <summary>일반 대검: 자루 · 코등이 · 강철 날 · 칼끝. pivot 은 손 위치.</summary>
        static void DrawGreatSword(Vector2 hand, float angleDeg, float U, float alpha)
        {
            Vector2 dir = Polar(angleDeg);
            Vector2 perp = new Vector2(-dir.y, dir.x);
            float length = U * 2.2f, width = U * 0.36f;
            DrawFxRect(Texture2D.whiteTexture, hand + dir * (U * 0.1f), U * 0.55f, U * 0.13f, WoodShaft, alpha, angleDeg);                          // 자루
            DrawFxRect(fxDisc, hand - dir * (U * 0.17f), U * 0.24f, U * 0.24f, new Color(0.75f, 0.62f, 0.3f), alpha);                                // 폼멜
            DrawFxRect(Texture2D.whiteTexture, hand + dir * (U * 0.4f), U * 0.14f, width * 2.4f, new Color(0.75f, 0.62f, 0.3f), alpha, angleDeg);   // 코등이
            Vector2 start = hand + dir * (U * 0.46f);
            Vector2 mid = start + dir * (length * 0.42f);
            DrawFxRect(Texture2D.whiteTexture, mid, length * 0.84f, width, SteelBlade, alpha, angleDeg);
            DrawFxRect(Texture2D.whiteTexture, mid, length * 0.8f, width * 0.2f, new Color(0.55f, 0.57f, 0.68f), alpha, angleDeg);
            DrawFxRect(Texture2D.whiteTexture, mid + perp * (width * 0.4f), length * 0.84f, width * 0.12f, Color.white, alpha, angleDeg);
            DrawFxRect(Diamond, start + dir * (length * 0.84f), length * 0.3f, width, SteelBlade, alpha, angleDeg);
        }

        // ───────── 바닥 잔흔 (유닛보다 아래) ─────────

        static Material fillMaterial;

        static void DrawGroundEffects(Rect field, float unit, FieldBattle b)
        {
            EnsureFx();
            foreach (var e in b.Effects)
            {
                if (e.Kind != FieldEffectKind.WhiteSlash) continue;
                float age = b.Time - e.Start - e.Delay;
                float len = SpecialLength(e);
                if (age < 0f || age >= len) continue;
                DrawSkyTrace(field, unit, e, age, age / len);
            }
        }

        /// <summary>
        /// 하늘가르기 잔흔 (사용자 2026-11-27): 검이 지나간 각도만큼의 부채꼴을 안까지 채우고(바깥은 진하게, 안은 투명하게),
        /// 반지름은 직선 잔흔 길이만큼 커진다. 검의 마지막 각도 = 직선 잔흔 방향.
        /// </summary>
        static void DrawSkyTrace(Rect field, float unit, FieldEffect e, float age, float t)
        {
            Vector2 P(Vector2 v) => new Vector2(field.x + v.x * unit, field.y + v.y * unit);
            Vector2 from = P(e.Origin);
            Vector2 dir = (P(e.End) - from).sqrMagnitude > 1f ? (P(e.End) - from).normalized : e.Dir;
            // 잔흔 사거리는 항상 같다 (사용자 2026-11-27): 스킬 사거리 그대로. 필드 밖으로 나가는 부분은 잘라서 그린다
            float full = Vector2.Distance(from, P(e.End));
            float fade = t < 0.45f ? 1f : 1f - (t - 0.45f) / 0.55f;
            float grow = Ease(Mathf.Clamp01(age / (SkyCutTime + 0.06f)));
            float radius = Mathf.Lerp(unit * 3f, full, grow);                          // 베면서 잔흔 거리까지 커진다

            float start = SkyCutAngle(e.Dir, 0f);
            float swept = Mathf.DeltaAngle(start, SkyCutAngle(e.Dir, age));
            if (Mathf.Abs(swept) > 1f) FillSector(field, from, radius, start, swept, fade);

            // 직선 잔흔
            float line = radius;
            float hitWidth = e.Radius * 2f * unit;                                     // 실제 판정 폭
            if (age > SkyCutTime * 0.6f)
            {
                Vector2 head = from + dir * Mathf.Min(line, DistanceToEdge(field, from, dir));
                DrawLine(from, head, hitWidth, Color.white, 0.22f * fade);
                DrawLine(from, head, hitWidth * 0.34f, Color.white, 0.75f * fade);
                DrawLine(from, head, hitWidth * 0.1f, Color.white, fade);
            }
        }

        /// <summary>
        /// 중심에서 radius 까지 채운 부채꼴. 바깥 가장자리는 불투명, 안쪽으로 갈수록 투명. 필드 밖은 가장자리에서 자른다.
        /// </summary>
        static void FillSector(Rect field, Vector2 center, float radius, float startDeg, float sweepDeg, float fade)
        {
            if (Event.current.type != EventType.Repaint) return;
            if (fillMaterial == null)
            {
                var shader = Shader.Find("Hidden/Internal-Colored");
                if (shader == null) return;
                fillMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
                fillMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                fillMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                fillMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
                fillMaterial.SetInt("_ZWrite", 0);
                fillMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
            }

            const int Seg = 40;
            // 반지름 비율별 불투명도: 안 → 밖
            float[] at = { 0f, 0.45f, 0.72f, 0.9f, 0.975f, 1f };
            float[] alpha = { 0.02f, 0.08f, 0.22f, 0.55f, 0.95f, 0.85f };

            GL.PushMatrix();
            fillMaterial.SetPass(0);
            GL.LoadOrtho();
            GL.Begin(GL.TRIANGLES);
            // 점 = (x, y, 불투명도). 원 모양은 그대로 두고 필드 밖 부분만 잘라 낸다.
            Vector3 Point(float deg, int ring)
            {
                Vector2 p = center + Polar(deg) * (radius * at[ring]);
                return new Vector3(p.x, p.y, alpha[ring] * fade);
            }
            for (int i = 0; i < Seg; i++)
            {
                float a0 = startDeg + sweepDeg * i / Seg, a1 = startDeg + sweepDeg * (i + 1) / Seg;
                for (int ring = 0; ring < at.Length - 1; ring++)
                {
                    EmitClipped(field, Point(a0, ring), Point(a0, ring + 1), Point(a1, ring + 1));
                    EmitClipped(field, Point(a0, ring), Point(a1, ring + 1), Point(a1, ring));
                }
            }
            GL.End();
            GL.PopMatrix();
        }

        static readonly System.Collections.Generic.List<Vector3> clipA = new System.Collections.Generic.List<Vector3>(8);
        static readonly System.Collections.Generic.List<Vector3> clipB = new System.Collections.Generic.List<Vector3>(8);

        /// <summary>삼각형(x, y, 알파)을 area 사각형으로 잘라 GL 에 보낸다 (GL.TRIANGLES 안에서 호출).</summary>
        static void EmitClipped(Rect area, Vector3 a, Vector3 b, Vector3 c)
        {
            clipA.Clear(); clipA.Add(a); clipA.Add(b); clipA.Add(c);
            for (int edge = 0; edge < 4 && clipA.Count > 0; edge++)
            {
                clipB.Clear();
                for (int i = 0; i < clipA.Count; i++)
                {
                    Vector3 p = clipA[i], q = clipA[(i + 1) % clipA.Count];
                    float dp = Inside(area, p, edge), dq = Inside(area, q, edge);
                    if (dp >= 0f) clipB.Add(p);
                    if ((dp >= 0f) != (dq >= 0f)) clipB.Add(Vector3.Lerp(p, q, dp / (dp - dq)));
                }
                clipA.Clear();
                clipA.AddRange(clipB);
            }
            for (int i = 1; i + 1 < clipA.Count; i++)
            {
                GlVertex(clipA[0]); GlVertex(clipA[i]); GlVertex(clipA[i + 1]);
            }
        }

        static float Inside(Rect area, Vector3 p, int edge)
            => edge == 0 ? p.x - area.x : edge == 1 ? area.xMax - p.x : edge == 2 ? p.y - area.y : area.yMax - p.y;

        static void GlVertex(Vector3 v)
        {
            GL.Color(new Color(1f, 1f, 1f, v.z));
            GL.Vertex3(v.x / vw, 1f - v.y / vh, 0f);
        }

        static void DrawSpecial(Rect field, float unit, float U, FieldBattle b, FieldEffect e, Vector2 c, Vector2 follow,
                                float t, float age, System.Func<Vector2, Vector2> P)
        {
            Color main = e.Color, light = Light(main), core = Core(main);
            float dirAng = AngleOf(e.Dir);
            float R = Mathf.Max(0.3f, e.Radius) * U;
            switch (e.Kind)
            {
                case FieldEffectKind.BladeSlam:   // 머리 위에서 앞으로 내려찍는 직사각형 대검
                {
                    if (IsStium(e) && e.Target != null) { DrawStiumSmashCast(e, follow, unit, age); break; }
                    bool longBeam = e.Radius >= 0.9f && e.Target == null;
                    float length = longBeam ? Mathf.Min(12f * unit, Vector2.Distance(P(e.Origin), P(e.End))) : U * 2.2f;
                    float swing = Mathf.Clamp01(t / 0.6f);
                    float eased = swing * swing;
                    float from = -90f, to = dirAng;
                    float ang = Mathf.LerpAngle(from, to, eased);
                    float a = t < 0.8f ? 1f : (1f - t) / 0.2f;
                    if (longBeam) DrawBlade(follow, ang, length, U * 0.75f, main, a);   // 길게 뻗는 날은 도형 유지
                    else DrawSkillBlade(e, unit, follow, ang, length, U * 0.5f, main, a);
                    if (swing >= 1f)
                    {
                        Vector2 tip = follow + new Vector2(Mathf.Cos(to * Mathf.Deg2Rad), Mathf.Sin(to * Mathf.Deg2Rad)) * (length * 0.85f);
                        float k = (t - 0.6f) / 0.4f;
                        DrawFx(fxRing, tip, U * Mathf.Lerp(0.8f, 3f, k), light, 1f - k);
                        DrawFx(fxDisc, tip, U * 1.6f, main, 0.7f * (1f - k));
                    }
                    break;
                }
                case FieldEffectKind.BladeRaise:  // 수직으로 들어 올린 대검 (모으기)
                {
                    float grow = Ease(Mathf.Clamp01(t * 2f));
                    float length = U * Mathf.Lerp(1.2f, 3.4f, grow);
                    float shake = Mathf.Sin(age * 60f) * 1.5f * t;
                    bool stium = e.Source != null && e.Source.Def != null && e.Source.Def.Id == "stium";
                    // 도트는 늘어나지 않으므로 처음부터 다 자란 크기로
                    DrawSkillBlade(e, unit, follow + new Vector2(shake, -U * 0.3f), -90f, stium ? U * 3.4f : length, U * 0.7f, main, 1f);
                    for (int k = 0; k < 5; k++)
                    {
                        float s = Mathf.Repeat(t * 3f + k / 5f, 1f);
                        DrawFx(fxDisc, follow + new Vector2((Hash(e.Start, k) - 0.5f) * U, -U * 0.4f - s * length), U * 0.3f, light, 1f - s);
                    }
                    break;
                }
                case FieldEffectKind.BladeSwing:  // 대검을 옆으로 휘둘러 베기
                {
                    bool full = e.Count >= 360;      // 전방위 베기: 한 바퀴
                    float sweep = full ? 360f : e.Count > 0 ? Mathf.Min(220f, e.Count) : 150f;
                    float ang = dirAng - sweep * 0.5f + sweep * Ease(t);
                    float length = full ? e.Radius * unit : Mathf.Max(U * 2f, R);
                    float a = t < 0.75f ? 1f : (1f - t) / 0.25f;
                    if (full)
                    {
                        // 지나간 자리에 잔상 호 + 범위 고리
                        for (int k = 1; k <= 4; k++)
                            if (sweep * Ease(t) > k * 55f)
                                DrawFx(fxArc, follow, length * 2.2f, main, 0.55f * a * (1f - k * 0.18f), ang - k * 55f);
                        DrawFx(fxRing, follow, length * 2.3f * Mathf.Lerp(0.6f, 1f, Ease(t)), light, 0.6f * (1f - t));
                    }
                    DrawFx(fxArc, follow, length * 2.3f, light, 0.8f * a, ang - 18f);
                    DrawFx(fxArc, follow, length * 2.0f, main, 0.6f * a, ang - 38f);
                    DrawSkillBlade(e, unit, follow, ang, length, U * 0.42f, main, a);
                    break;
                }
                case FieldEffectKind.BigSlash:    // 큰 베기 한 번
                {
                    float ang = dirAng - 80f + 160f * Ease(t);
                    float size = R * 2.6f;
                    Glow(follow, size * 0.7f, main, 1f - t);
                    DrawFx(fxArc, follow, size, light, 1f - t, ang);
                    DrawFx(fxArc, follow, size * 1.08f, main, 0.8f * (1f - t), ang - 20f);
                    DrawFx(fxArc, follow, size * 0.86f, Color.white, 0.7f * (1f - t), ang + 12f);
                    break;
                }
                case FieldEffectKind.Whirl:       // 자신을 중심으로 화염 참격이 계속 생긴다
                {
                    const float step = 0.1f, one = 0.28f;
                    int last = Mathf.FloorToInt(age / step);
                    for (int k = Mathf.Max(0, last - 3); k <= last; k++)
                    {
                        float s = (age - k * step) / one;
                        if (s < 0f || s >= 1f) continue;
                        float ang = k * 137f;                    // 고르게 돌아가며
                        float rr = R * (0.45f + 0.55f * Hash(e.Start, k));
                        Vector2 pos = follow + new Vector2(Mathf.Cos(ang * Mathf.Deg2Rad), Mathf.Sin(ang * Mathf.Deg2Rad)) * rr * 0.5f;
                        DrawFx(fxArc, pos, R * 1.5f, new Color(1f, 0.85f, 0.35f), 1f - s, ang + 120f * s);
                        DrawFx(fxArc, pos, R * 1.65f, main, 0.8f * (1f - s), ang - 25f + 120f * s);
                    }
                    DrawFx(fxRing, follow, R * 2.1f, main, 0.35f * Mathf.Min(1f, (1f - t) * 4f));
                    break;
                }
                case FieldEffectKind.Coin:        // 동전이 튀어 올랐다 떨어진다
                {
                    float h = Mathf.Sin(t * Mathf.PI) * unit * 2.4f;
                    Vector2 pos = follow + new Vector2(t * U * 0.6f, -h - U * 0.5f);
                    float flip = Mathf.Abs(Mathf.Cos(age * 18f));
                    DrawFxRect(fxDisc, pos, U * 1.1f, U * 1.1f, new Color(1f, 0.85f, 0.3f), 0.5f);
                    DrawFxRect(fxRing, pos, U * 0.65f * Mathf.Max(0.15f, flip), U * 0.65f, Color.white, 1f);
                    DrawFxRect(fxDisc, pos, U * 0.75f * Mathf.Max(0.15f, flip), U * 0.75f, new Color(1f, 0.82f, 0.2f), 1f);
                    break;
                }
                case FieldEffectKind.Freeze:      // 필드가 얼고 적들이 얼음에 갇힌 듯한 연출
                {
                    float a = Mathf.Min(1f, age / 0.15f) * Mathf.Min(1f, (1f - t) * 2.5f);
                    DrawFxRect(Texture2D.whiteTexture, field.center, field.width, field.height, new Color(0.55f, 0.85f, 1f), 0.22f * a);
                    DrawSheet("frostNova", Feet(follow, unit), unit, age, Color.white);
                    for (int k = 0; k < 24; k++)  // 바닥 서리
                    {
                        Vector2 pos = new Vector2(field.x + Hash(e.Start, k) * field.width, field.y + Hash(e.Start, k + 40) * field.height);
                        DrawFxRect(fxLine, pos, unit * 1.2f, unit * 0.12f, Color.white, 0.5f * a, Hash(e.Start, k + 80) * 180f);
                        DrawFxRect(fxLine, pos, unit * 0.8f, unit * 0.1f, Color.white, 0.4f * a, Hash(e.Start, k + 80) * 180f + 60f);
                    }
                    if (e.Units == null) break;
                    foreach (var u in e.Units)
                    {
                        if (u == null || !u.Alive) continue;
                        Vector2 pos = P(u.Pos);
                        float s = unit * 1.35f;
                        DrawFxRect(Texture2D.whiteTexture, pos, s, s, new Color(0.7f, 0.92f, 1f), 0.45f * a);
                        DrawFxRect(Texture2D.whiteTexture, pos + new Vector2(-s * 0.18f, -s * 0.2f), s * 0.18f, s * 0.5f, Color.white, 0.6f * a, 30f);
                        DrawFx(fxRing, pos, s * 1.5f, Color.white, 0.5f * a);
                    }
                    break;
                }
                case FieldEffectKind.Grab:        // 잡은 대상을 원으로 감싼다
                {
                    float pulse = 1f + 0.08f * Mathf.Sin(age * 14f);
                    float appear = Mathf.Clamp01(age / 0.15f);
                    var held = e.Target;
                    if (held != null && held.AirTimer > 0f && held.AirTotal > 0f)
                        c.y -= held.AirHeight * unit * Mathf.Sin(Mathf.PI * Mathf.Clamp01(1f - held.AirTimer / held.AirTotal));
                    DrawFx(fxDisc, c, U * 2.2f * pulse, new Color(0.25f, 0.25f, 0.3f), 0.45f);
                    DrawFx(fxRing, c, U * Mathf.Lerp(3f, 1.8f, appear) * pulse, main, 1f);
                    DrawFx(fxRing, c, U * 1.5f * pulse, light, 0.8f);
                    DrawLine(follow, c, U * 0.3f, main, 0.6f);
                    break;
                }
                case FieldEffectKind.Missiles:    // 등의 케이스를 앞에 수직으로 내려박고 → 미사일 수직 상승 → 폭격 (사용자 2026-11-27)
                {
                    if (e.Points == null) break;
                    Vector2 self = P(e.Origin);
                    float side = e.Dir.x >= 0f ? 1f : -1f;
                    Vector2 planted = self + new Vector2(side * unit * 1.15f, -unit * 0.15f);
                    float total = SpecialLength(e);
                    float caseTotal = total - Mathf.Max(0f, SheetTime("boom") - FxMissileBoom);   // 케이스는 폭발 잔연보다 먼저 사라진다
                    float ca = Mathf.Clamp01((caseTotal - age) / 0.3f);
                    float boomLength = SheetTime("boom");

                    // 케이스: 등 뒤에서 머리 위로 넘겨 앞에 세로로 내려박는다
                    float k0 = Mathf.Clamp01(age / FxCasePlant);
                    float fall = k0 * k0;
                    Vector2 back = self + new Vector2(-side * unit * 0.55f, -unit * 0.1f);
                    Vector2 at = Vector2.Lerp(back, planted, fall);
                    at.y -= Mathf.Sin(fall * Mathf.PI) * unit * 1.6f;
                    float spin = (1f - fall) * -120f * side;
                    float cw = unit * 0.55f, ch = unit * 1.35f;
                    DrawFxRect(Texture2D.whiteTexture, at, cw, ch, new Color(0.35f, 0.33f, 0.3f), ca, spin);
                    DrawFxRect(Texture2D.whiteTexture, at, cw * 0.18f, ch * 0.92f, new Color(0.65f, 0.58f, 0.45f), ca, spin);
                    DrawFxRect(Texture2D.whiteTexture, at, cw * 1.05f, ch * 0.1f, new Color(0.55f, 0.5f, 0.4f), ca, spin);
                    if (age > FxCasePlant && age < FxCasePlant + 0.35f)      // 내려박는 충격 · 갈라진 바닥
                    {
                        float q = (age - FxCasePlant) / 0.35f;
                        Vector2 ground = planted + new Vector2(0f, ch * 0.5f);
                        DrawFxRect(fxRing, ground, unit * (1f + 3.5f * Ease(q)), unit * (0.35f + 1.2f * Ease(q)), light, 1f - q);
                        for (int k = -2; k <= 2; k++)
                            if (k != 0) DrawFxRect(fxLine, ground + new Vector2(k * unit * 0.42f, Mathf.Abs(k) * unit * 0.07f), unit * 0.7f, unit * 0.1f, Color.white, 1f - q, k * 14f);
                    }
                    if (age < FxCasePlant) break;

                    Vector2 muzzle = planted + new Vector2(0f, -ch * 0.55f);
                    // 상승 높이: 발사구 위 MissileRiseHeight 칸 (사용자 2026-11-27: 낮추기). 필드 밖으로는 안 나간다.
                    float apexY = Mathf.Max(field.y + unit * 0.45f, muzzle.y - unit * MissileRiseHeight);
                    float fireAge = age - FxCasePlant;
                    for (int k = 0; k < e.Points.Length; k++)
                    {
                        float s = fireAge - k * FxMissileGap;
                        if (s < 0f) continue;
                        // 발사관 위치에서 곧게 위로
                        Vector2 mouth = muzzle + new Vector2((k % 3 - 1) * unit * 0.14f, 0f);
                        Vector2 apex = new Vector2(mouth.x, Mathf.Min(apexY, mouth.y - unit * 0.5f));
                        Vector2 hit = P(e.Points[k]) + new Vector2((Hash(e.Start, k + 3) - 0.5f) * unit, (Hash(e.Start, k + 9) - 0.5f) * unit * 0.6f);
                        if (s < FxMissileRise)
                        {
                            float f = Ease(s / FxMissileRise);
                            Vector2 pos = Vector2.Lerp(mouth, apex, f);
                            DrawLine(Vector2.Lerp(mouth, apex, Mathf.Max(0f, f - 0.45f)), pos, unit * 0.24f, main, 0.8f);
                            DrawFxRect(Texture2D.whiteTexture, pos, unit * 0.16f, unit * 0.42f, Color.white, 1f);
                            if (f < 0.25f) DrawFx(fxDisc, mouth, unit * 0.9f, main, 1f - f * 4f);     // 발사 섬광
                        }
                        else if (s < FxMissileRise + FxMissileFall)
                        {
                            float f = (s - FxMissileRise) / FxMissileFall;
                            // 상승 끝에서 포물선을 그리며 대상에게 떨어진다 (2차 베지어: 조정점은 두 점 가운데 위)
                            Vector2 ctrl = new Vector2((apex.x + hit.x) * 0.5f,
                                                       Mathf.Max(field.y + unit * 0.2f, Mathf.Min(apex.y, hit.y) - unit * 1.5f));
                            float g = f * (0.6f + 0.4f * f);                 // 뒤로 갈수록 빨라진다
                            Vector2 pos = Arc(apex, ctrl, hit, g);
                            Vector2 prev = pos;
                            for (int q = 1; q <= 6; q++)                     // 곡선 꼬리
                            {
                                Vector2 tailPoint = Arc(apex, ctrl, hit, Mathf.Max(0f, g - q * 0.05f));
                                DrawLine(tailPoint, prev, unit * 0.24f * (1f - q * 0.12f), main, 0.85f * (1f - q * 0.13f));
                                prev = tailPoint;
                            }
                            DrawFx(fxDisc, pos, unit * 0.45f, light, 1f);
                            DrawFx(fxRing, hit, unit * 0.9f, new Color(1f, 0.3f, 0.2f), 0.5f);
                        }
                        else if (s < FxMissileRise + FxMissileFall + boomLength)
                        {
                            DrawSheet("boom", Feet(hit, unit), unit, s - FxMissileRise - FxMissileFall, Color.white);
                        }
                    }
                    break;
                }
                case FieldEffectKind.Punches:     // 연속 주먹
                {
                    int n = Mathf.Max(1, e.Count);
                    float step = Mathf.Max(0.05f, e.Duration) / n;
                    for (int k = 0; k < n; k++)
                    {
                        float s = (age - k * step) / (step * 1.4f);
                        if (s < 0f || s >= 1f) continue;
                        Vector2 d = c - follow;
                        Vector2 perp = d.sqrMagnitude > 0.1f ? new Vector2(-d.y, d.x).normalized : Vector2.up;
                        Vector2 to = c + perp * ((k % 2 == 0 ? 1f : -1f) * U * 0.35f);
                        float f = Ease(Mathf.Min(1f, s * 1.8f));
                        Vector2 pos = Vector2.Lerp(follow, to, f);
                        DrawLine(Vector2.Lerp(follow, to, Mathf.Max(0f, f - 0.5f)), pos, U * 0.55f, main, 0.7f * (1f - s));
                        DrawFx(fxDisc, pos, U * 0.8f, light, 1f - s);
                        DrawFxRect(Texture2D.whiteTexture, pos, U * 0.34f, U * 0.3f, Color.white, 0.9f * (1f - s), AngleOf(to - follow));
                        if (s > 0.45f) DrawFx(fxRing, to, U * 1.7f * (s - 0.3f), main, 1f - s);
                    }
                    break;
                }
                case FieldEffectKind.SlantPillar: // 비스듬히 솟는 불기둥
                {
                    float a = 1f - t;
                    float h = U * 3.8f * Ease(Mathf.Min(1f, t * 3f));
                    float lean = e.Dir.x >= 0f ? 28f : -28f;
                    Vector2 up = new Vector2(Mathf.Sin(lean * Mathf.Deg2Rad), -Mathf.Cos(lean * Mathf.Deg2Rad));
                    Vector2 mid = c + up * (h * 0.5f);
                    float ang = AngleOf(up);
                    DrawFxRect(fxLine, mid, h * 1.1f, U * 1.5f, main, 0.55f * a, ang);
                    DrawFxRect(fxLine, mid, h, U * 0.9f, new Color(1f, 0.7f, 0.2f), 0.9f * a, ang);
                    DrawFxRect(fxLine, mid, h * 0.9f, U * 0.35f, Color.white, a, ang);
                    DrawFx(fxDisc, c, U * 1.8f, main, 0.8f * a);
                    break;
                }
                case FieldEffectKind.CrossHold:   // 십자가를 들어 올림
                    DrawCross(follow + new Vector2(e.Dir.x >= 0f ? U * 0.35f : -U * 0.35f, -U * 1.2f), U * 2.2f, main, Mathf.Min(1f, age / 0.1f));
                    DrawFx(fxRing, follow, U * 2f, light, 0.6f);
                    break;
                case FieldEffectKind.CrossSlam:   // 휘둘러 내려찍기
                {
                    float swing = Mathf.Clamp01(t / 0.5f);
                    float tilt = Mathf.Lerp(-110f, 40f, swing * swing) * (e.Dir.x >= 0f ? 1f : -1f);
                    float a = t < 0.7f ? 1f : (1f - t) / 0.3f;
                    Vector2 arm = new Vector2(Mathf.Sin(tilt * Mathf.Deg2Rad), -Mathf.Cos(tilt * Mathf.Deg2Rad)) * (U * 1.2f);
                    DrawCross(c + arm - new Vector2(0f, U * 0.2f), U * 2.4f, main, a, tilt);
                    if (swing >= 1f)
                    {
                        float k = (t - 0.5f) / 0.5f;
                        DrawFx(fxRing, c, U * Mathf.Lerp(1f, 4f, Ease(k)), light, 1f - k);
                        DrawFx(fxDisc, c, U * 2.4f, main, 0.6f * (1f - k));
                    }
                    break;
                }
                case FieldEffectKind.CrossFall:   // 대상 위에 십자가 소환 → 준비가 끝나면 낙하
                {
                    float hold = e.Duration;
                    float dropStart = hold - FxCrossFallDrop;
                    float height = U * 3f;
                    Vector2 hover = c + new Vector2(0f, -unit * 2.6f + Mathf.Sin(age * 5f) * 3f);
                    if (age < dropStart)
                    {
                        float appear = Mathf.Clamp01(age / 0.3f);
                        DrawCross(hover, height * appear, main, appear);
                        DrawFx(fxRing, c, U * Mathf.Lerp(3.6f, 1.6f, age / Mathf.Max(0.1f, dropStart)), light, 0.7f);
                    }
                    else if (age < hold)
                    {
                        float f = (age - dropStart) / FxCrossFallDrop;
                        Vector2 pos = Vector2.Lerp(hover, c + new Vector2(0f, -height * 0.25f), f * f);
                        DrawLine(hover, pos, U * 0.8f, main, 0.5f);
                        DrawCross(pos, height * 1.1f, main, 1f);
                    }
                    else
                    {
                        float f = (age - hold) / FxCrossFallAfter;
                        DrawCross(c + new Vector2(0f, -height * 0.25f), height * 1.1f, main, 1f - f);
                        DrawFx(fxRing, c, U * 5f * Ease(f), light, 1f - f);
                        DrawFx(fxDisc, c, U * 3f, light, 0.8f * (1f - f));
                    }
                    break;
                }
                case FieldEffectKind.Maw:         // 거대한 이빨 입이 벌어졌다가 삼킨다
                {
                    float size = U * 3.4f;
                    float closing = Mathf.Clamp01((age - MawOpen) / MawClose);
                    float open = age < MawOpen ? Ease(age / MawOpen) : 1f - closing * closing;   // 닫힘은 가속하며 순식간에
                    float closedFor = age - MawOpen - MawClose;                                   // 닫힌 뒤 지난 시간
                    float a = closedFor <= MawHold ? 1f : 1f - Mathf.Clamp01((closedFor - MawHold) / MawFade);
                    float gap = size * 0.42f * open + size * 0.06f;
                    var jaw = new Color(0.12f, 0.06f, 0.14f);
                    DrawFx(fxDisc, c, size * 1.5f, main, 0.35f * a);
                    for (int side = -1; side <= 1; side += 2)
                    {
                        Vector2 j = c + new Vector2(0f, side * (gap + size * 0.16f));
                        DrawFxRect(fxDisc, j, size * 1.5f, size * 0.75f, jaw, a);
                        DrawFxRect(Texture2D.whiteTexture, j, size * 0.95f, size * 0.22f, jaw, a);
                        for (int k = -2; k <= 2; k++)   // 안쪽 이빨
                        {
                            float tooth = size * 0.13f;
                            Vector2 tp = c + new Vector2(k * size * 0.145f, side * (gap + Mathf.Abs(k) * size * 0.006f));
                            DrawFxRect(Texture2D.whiteTexture, tp, tooth * 0.7f, tooth * 0.7f, Color.white, a, 45f);
                        }
                        for (int k = -1; k <= 1; k += 2)   // 가장자리 송곳니: 긴 마름모 (위아래가 살짝 엇갈려 맞물린다)
                        {
                            Vector2 tp = c + new Vector2(k * size * 0.435f + side * size * 0.035f, side * (gap + size * 0.02f));
                            DrawFxRect(Diamond, tp, size * 0.15f, size * 0.46f, Color.white, a);
                        }
                    }
                    if (closing >= 1f && closedFor < 0.2f)   // 맞물리는 순간의 충격
                        DrawFx(fxRing, c, size * (0.8f + closedFor * 6f), light, 1f - closedFor / 0.2f);
                    break;
                }
                case FieldEffectKind.ForwardSlashes:  // 전방으로 연속 베기
                {
                    int n = Mathf.Max(1, e.Count);
                    float step = Mathf.Max(0.05f, e.Duration) / n;
                    Vector2 fwd = e.Dir;
                    Vector2 side = new Vector2(-fwd.y, fwd.x);
                    for (int k = 0; k < n; k++)
                    {
                        float s = (age - k * step) / 0.3f;
                        if (s < 0f || s >= 1f) continue;
                        float reach = 0.6f + Hash(e.Start, k) * 2.2f;
                        Vector2 pos = follow + fwd * (reach * unit) + side * ((Hash(e.Start, k + 31) - 0.5f) * unit * reach * 1.1f);
                        float ang = AngleOf(fwd) + (k % 2 == 0 ? -55f : 55f) + (k % 2 == 0 ? 110f : -110f) * Ease(s);
                        Glow(pos, U * 1.6f, main, 1f - s);
                        DrawFx(fxArc, pos, U * 2.4f, light, 1f - s, ang);
                        DrawFx(fxArc, pos, U * 2.6f, main, 0.7f * (1f - s), ang - 22f);
                        DrawFx(fxDisc, pos, U * 0.9f, core, 0.7f * (1f - s));
                    }
                    break;
                }
                case FieldEffectKind.FanWave:     // 부채꼴 충격파
                {
                    float ang = dirAng;
                    for (int k = 0; k < 3; k++)
                    {
                        float s = Mathf.Clamp01(t * 1.6f - k * 0.18f);
                        if (s <= 0f) continue;
                        float size = unit * Mathf.Lerp(1.5f, 8f - k * 1.2f, Ease(s));
                        DrawFx(fxArc, c, size, k == 0 ? Color.white : k == 1 ? light : main, 1f - s, ang);
                        DrawFx(fxArc, c, size * 0.96f, main, 0.7f * (1f - s), ang);
                    }
                    break;
                }
                case FieldEffectKind.FireLine:    // 맵을 꿰뚫는 직선 화염 (필드 안으로 잘라 그린다)
                {
                    // 맵 전체 관통 (사용자 2026-11-26): 시전자를 지나 한쪽 끝에서 반대쪽 끝까지
                    Vector2 origin = P(e.Origin);
                    Vector2 dir = e.Dir;
                    Vector2 from = origin - dir * DistanceToEdge(field, origin, -dir);
                    Vector2 to = origin + dir * DistanceToEdge(field, origin, dir);
                    float a = t < 0.5f ? 1f : (1f - t) * 2f;
                    float w = U * (1.9f + 0.2f * Mathf.Sin(age * 50f));
                    DrawLine(from, to, w * 1.9f, new Color(0.9f, 0.2f, 0f), 0.55f * a);
                    DrawLine(from, to, w * 1.2f, main, 0.95f * a);
                    DrawLine(from, to, w * 0.65f, new Color(1f, 0.85f, 0.3f), a);
                    DrawLine(from, to, w * 0.25f, Color.white, a);
                    for (int k = 0; k < 42; k++)   // 불꽃 혀
                    {
                        float s = (k + 0.5f) / 42f;
                        Vector2 side = new Vector2(-dir.y, dir.x) * ((Hash(e.Start, k) - 0.5f) * w * 1.5f);
                        DrawFx(fxDisc, Vector2.Lerp(from, to, s) + side, U * (0.5f + Hash(e.Start, k + 60) * 0.7f),
                               k % 3 == 0 ? new Color(1f, 0.8f, 0.2f) : main, 0.85f * a);
                    }
                    break;
                }
                case FieldEffectKind.Afterimage:  // 지나간 자리에 몸 모양 잔상 (뒤쪽일수록 먼저 사라진다)
                {
                    Vector2 a = P(e.Origin), z = P(e.End);
                    float travel = Mathf.Max(0.05f, e.Duration);
                    float body = FieldConst.Radius * 2f * unit;
                    for (int k = 0; k < GhostCount; k++)
                    {
                        float s = k / (float)GhostCount;
                        float since = age - travel * s;
                        if (since < 0f || since > GhostLinger) continue;
                        float fade = 1f - since / GhostLinger;
                        Vector2 pos = Vector2.Lerp(a, z, s);
                        DrawFx(fxDisc, pos, body * 1.9f, main, 0.35f * fade);
                        DrawFxRect(Texture2D.whiteTexture, pos, body, body, new Color(main.r * 0.6f, main.g * 0.4f, main.b), 0.7f * fade);
                        DrawFxRect(Texture2D.whiteTexture, pos, body * 0.55f, body * 0.55f, light, 0.45f * fade);
                    }
                    if (age < travel)
                        DrawLine(a, Vector2.Lerp(a, z, age / travel), U * 0.3f, main, 0.7f);
                    break;
                }
                case FieldEffectKind.Thrust:      // 도달 순간 찌르기: 날카로운 창끝이 대상을 꿰뚫는다
                {
                    Vector2 dir = e.Dir.sqrMagnitude > 0.001f ? e.Dir.normalized : Vector2.right;
                    float grow = Ease(Mathf.Clamp01(t / 0.2f));
                    float a = t < 0.45f ? 1f : (1f - t) / 0.55f;
                    float ang = AngleOf(dir);
                    Vector2 tail = c - dir * (U * 2.2f), head = c + dir * (U * 2.6f * grow);
                    Vector2 mid = (tail + head) * 0.5f;
                    float len = Vector2.Distance(tail, head);
                    DrawFxRect(Diamond, mid, len * 1.15f, U * 1.05f, main, 0.6f * a, ang);
                    DrawFxRect(Diamond, mid, len, U * 0.55f, light, a, ang);
                    DrawFxRect(Diamond, mid, len * 0.95f, U * 0.2f, Color.white, a, ang);
                    for (int side = -1; side <= 1; side += 2)      // 옆으로 갈라지는 충격선
                    {
                        float sa = ang + side * 32f;
                        Vector2 d2 = new Vector2(Mathf.Cos(sa * Mathf.Deg2Rad), Mathf.Sin(sa * Mathf.Deg2Rad));
                        DrawFxRect(Diamond, c + d2 * (U * 1.1f * grow), U * 1.8f * grow, U * 0.2f, light, 0.8f * a, sa);
                    }
                    DrawFx(fxRing, c, U * Mathf.Lerp(0.6f, 3.2f, Ease(t)), light, 1f - t);
                    DrawFx(fxDisc, c, U * 1.4f, Color.white, 0.7f * (1f - Mathf.Clamp01(t * 2.5f)));
                    break;
                }
                case FieldEffectKind.IceShatter:  // 얼음 조각이 대상 뒤(탄 진행 방향)로 흩뿌려진다
                    DrawSheet("iceShatter", Feet(c, unit), unit, age, Color.white, e.Dir.x < 0f);
                    break;
                case FieldEffectKind.FistSlam:    // 큰 주먹이 원형 궤적을 그리며 위에서 아래로 내려찍는다
                {
                    float side = e.Dir.x >= 0f ? 1f : -1f;
                    float radius = U * 2.1f;
                    Vector2 center = c + new Vector2(-side * radius, 0f);   // 원의 중심: 찍는 지점의 옆
                    const float Swing = 0.34f;                              // 전체 중 휘두르는 비율
                    DrawSheet("slam", Feet(c, unit), unit, age - FistSwingTime * Swing, Color.white);
                    if (age >= FistSwingTime) break;                        // 이후는 착탄 시트만 남는다
                    t = age / FistSwingTime;
                    float k = Mathf.Clamp01(t / Swing);
                    float ang = Mathf.Lerp(-130f, 0f, k * k);              // 0 = 찍는 지점
                    Vector2 At(float deg) => center + new Vector2(Mathf.Cos(deg * Mathf.Deg2Rad) * side, Mathf.Sin(deg * Mathf.Deg2Rad)) * radius;
                    float fade = t < 0.6f ? 1f : (1f - t) / 0.4f;

                    Vector2 prev = At(-130f);                               // 원형 궤적
                    for (int i = 1; i <= 14; i++)
                    {
                        float d = Mathf.Lerp(-130f, ang, i / 14f);
                        Vector2 p = At(d);
                        float w = i / 14f;
                        DrawLine(prev, p, U * 0.85f * w, main, 0.6f * w * fade);
                        DrawLine(prev, p, U * 0.3f * w, light, 0.95f * w * fade);
                        prev = p;
                    }
                    Vector2 fist = At(ang);
                    float s = U * 1.15f;
                    float tilt = ang * side;
                    DrawFx(fxDisc, fist, s * 1.9f, main, 0.6f * fade);
                    DrawFxRect(Texture2D.whiteTexture, fist, s * 0.82f, s * 0.78f, light, fade, tilt);                      // 주먹
                    for (int i = -1; i <= 2; i++)                                                                            // 손가락 마디
                        DrawFxRect(fxDisc, fist + new Vector2((i - 0.5f) * s * 0.2f, s * 0.4f), s * 0.3f, s * 0.3f, Color.white, fade);
                    DrawFxRect(Texture2D.whiteTexture, fist + new Vector2(-side * s * 0.36f, 0f), s * 0.24f, s * 0.5f, main, fade, tilt);   // 엄지

                    if (k >= 1f)                                            // 착탄: 충격 고리 · 바닥 균열
                    {
                        float q = (t - Swing) / (1f - Swing);
                        DrawFx(fxDisc, c, U * 3f * Mathf.Lerp(0.5f, 1.1f, Ease(q)), Color.white, 0.75f * (1f - Mathf.Clamp01(q * 2.5f)));
                        DrawFxRect(fxRing, c, U * (1.5f + 4.5f * Ease(q)), U * (0.8f + 2.4f * Ease(q)), light, 1f - q);
                        DrawFxRect(fxRing, c, U * (1f + 3f * Ease(q)), U * (0.55f + 1.6f * Ease(q)), main, 1f - q);
                        for (int i = 0; i < 9; i++)
                        {
                            float a = i * 40f + Hash(e.Start, i) * 25f;
                            Vector2 d = Polar(a);
                            float len = U * (1f + Hash(e.Start, i + 11) * 1.4f) * Ease(Mathf.Min(1f, q * 4f));
                            DrawLine(c + d * (U * 0.4f), c + new Vector2(d.x, d.y * 0.6f) * (U * 0.4f + len), U * 0.16f, Color.white, 1f - q);
                        }
                    }
                    break;
                }
                case FieldEffectKind.SwordRaise:  // 대검을 머리 위로 들어 준비
                {
                    float up = Ease(Mathf.Clamp01(t * 2.5f));
                    float side = e.Dir.x >= 0f ? 1f : -1f;
                    float ang = -90f - side * Mathf.Lerp(-100f, 14f, up);
                    float shake = t > 0.5f ? Mathf.Sin(age * 55f) * 1.2f : 0f;
                    Vector2 hand = follow + new Vector2(shake, -U * Mathf.Lerp(0f, 0.55f, up));
                    DrawGreatSword(hand, ang, U, 1f);
                    if (t > 0.6f) DrawFx(fxDisc, hand + Polar(ang) * (U * 2.7f), U * (0.5f + 0.5f * Mathf.Sin(age * 20f)), Color.white, 0.9f);   // 칼끝 빛
                    break;
                }
                case FieldEffectKind.SwordCut:    // 들어 올린 대검을 앞으로 내려벤다
                {
                    float a = t < 0.7f ? 1f : (1f - t) / 0.3f;
                    DrawGreatSword(follow, SkyCutAngle(e.Dir, age), U, a);
                    break;
                }
                case FieldEffectKind.WhiteSlash:  // 바닥 잔흔: 유닛 아래에 그린다 (DrawGroundEffects)
                    break;
                case FieldEffectKind.LeapOff:     // 위로 솟구쳐 맵을 벗어남
                {
                    float rise = Ease(t);
                    Vector2 top = new Vector2(c.x, Mathf.Lerp(c.y, field.y, rise));
                    DrawLine(new Vector2(c.x, Mathf.Lerp(c.y, field.y, rise * rise)), top, U * 0.8f, main, 1f - t * 0.5f);
                    DrawLine(new Vector2(c.x, Mathf.Lerp(c.y, field.y, rise * rise)), top, U * 0.3f, Color.white, 1f - t * 0.5f);
                    DrawFx(fxRing, c, U * 3f * Ease(t), light, 1f - t);
                    break;
                }
                case FieldEffectKind.Landing:     // 그림자가 커지다가 낙하 · 충격
                {
                    if (age < e.Duration)
                    {
                        float k = age / Mathf.Max(0.1f, e.Duration);
                        DrawFxRect(fxDisc, c, U * Mathf.Lerp(0.8f, 2.8f, k), U * Mathf.Lerp(0.4f, 1.4f, k), Color.black, 0.8f);
                        DrawFx(fxRing, c, U * 2.6f, main, 0.35f + 0.35f * Mathf.Sin(age * 20f));
                        if (k > 0.7f)
                        {
                            float f = (k - 0.7f) / 0.3f;
                            Vector2 from = new Vector2(c.x, field.y);
                            Vector2 pos = Vector2.Lerp(from, c, f * f);
                            DrawLine(from, pos, U * 0.9f, main, 0.8f);
                            DrawFx(fxDisc, pos, U * 1.3f, light, 1f);
                        }
                    }
                    else DrawSheet("landing", Feet(c, unit), unit, age - e.Duration, Color.white);
                    break;
                }
            }
        }
    }
}
